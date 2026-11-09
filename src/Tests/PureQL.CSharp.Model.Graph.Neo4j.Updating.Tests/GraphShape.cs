namespace PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests;

/// <summary>
/// What the tests compare of a query graph: node and relationship counts, the sorted
/// record labels and relationship types, and the root's record label and id; empty
/// strings when nothing is stored.
/// </summary>
public sealed record GraphShape(
    long Nodes,
    long Relationships,
    string Labels,
    string Types,
    string Root,
    string Id
)
{
    /// <summary>Nothing stored.</summary>
    public static GraphShape Empty { get; } = new GraphShape(0, 0, "", "", "", "");

    /// <summary>The shape the Graph package writes for a query under an id.</summary>
    public static GraphShape Of(PureQLQuery query, Guid id)
    {
        QueryGraph graph = QueryGraphs.Of(query, id);
        return new GraphShape(
            graph.Nodes.Count,
            graph.Relationships.Count,
            string.Join(
                ",",
                graph.Nodes.Select(node => node.Labels[0]).Order(StringComparer.Ordinal)
            ),
            string.Join(
                ",",
                graph
                    .Relationships.Select(relationship => relationship.Type)
                    .Order(StringComparer.Ordinal)
            ),
            graph.Nodes[0].Labels[0],
            id.ToString("D")
        );
    }
}
