using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests;

/// <summary>A relationship as stored: its type, position and the child's element id.</summary>
internal sealed record StoredRelationship(string Type, long? Position, string To);

/// <summary>A node as stored, with its outgoing relationships.</summary>
internal sealed record StoredNode(
    string ElementId,
    IReadOnlyList<string> Labels,
    IReadOnlyDictionary<string, object> Properties,
    IReadOnlyList<StoredRelationship> Relationships
)
{
    /// <summary>The record label: neither <c>Node</c> nor <c>Query</c>.</summary>
    public string Label =>
        Labels.Single(label => label is not (GraphLabels.Node or GraphLabels.Query));
}

/// <summary>Every node stored under one query id, read back into the model.</summary>
internal sealed record StoredGraph(IReadOnlyList<StoredNode> Nodes)
{
    private const string Cypher = """
        MATCH (n:Node {queryId: $id})
        RETURN elementId(n) AS id, labels(n) AS labels, properties(n) AS props,
            [(n)-[r]->(m) | {type: type(r), position: r.position, to: elementId(m)}]
                AS out
        """;

    public static async Task<StoredGraph> ReadAsync(Neo4jFixture neo4j, Guid id)
    {
        List<StoredNode> nodes = [];
        foreach (IRecord record in await neo4j.RunAsync(Cypher, id))
        {
            nodes.Add(
                new StoredNode(
                    record["id"].As<string>(),
                    record["labels"].As<List<string>>(),
                    record["props"].As<Dictionary<string, object>>(),
                    [
                        .. record["out"]
                            .As<List<Dictionary<string, object>>>()
                            .Select(r => new StoredRelationship(
                                (string)r["type"],
                                r["position"] is long position ? position : null,
                                (string)r["to"]
                            )),
                    ]
                )
            );
        }

        return new StoredGraph(nodes);
    }

    /// <summary>The stored query as the Graph package reads it back.</summary>
    public PureQLQuery Query()
    {
        Dictionary<string, Dictionary<string, IReadOnlyList<IReadNode>>> children = [];
        Dictionary<string, IReadNode> read = [];
        foreach (StoredNode node in Nodes)
        {
            Dictionary<string, IReadOnlyList<IReadNode>> lists =
            [
                with(StringComparer.Ordinal),
            ];
            children[node.ElementId] = lists;
            read[node.ElementId] = new ReadNode(
                node.Label,
                node.Properties.ToDictionary(p => p.Key, p => (object?)p.Value),
                lists
            );
        }

        foreach (StoredNode node in Nodes)
        {
            foreach (
                IGrouping<string, StoredRelationship> type in node.Relationships.GroupBy(
                    r => r.Type
                )
            )
            {
                children[node.ElementId][type.Key] =
                [
                    .. type.OrderBy(r => r.Position ?? 0).Select(r => read[r.To]),
                ];
            }
        }

        StoredNode root = Nodes.Single(n => n.Labels.Contains(GraphLabels.Query));
        return QueryGraphs.Read(read[root.ElementId]);
    }
}
