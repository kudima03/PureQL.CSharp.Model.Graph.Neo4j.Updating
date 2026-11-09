using Neo4j.Driver;
using Testcontainers.Neo4j;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests;

/// <summary>
/// One Neo4j server for every integration test, started from <c>neo4j:5-community</c>
/// on a random host port, without authentication.
/// </summary>
public sealed class Neo4jFixture : IAsyncLifetime
{
    private readonly Neo4jContainer _container = new Neo4jBuilder(
        "neo4j:5-community"
    ).Build();

    private IDriver? _driver;

    public IDriver Driver =>
        _driver ?? throw new InvalidOperationException("The server is not started.");

    public IAsyncSession Session()
    {
        return Driver.AsyncSession();
    }

    /// <summary>The uniqueness constraint the Creation package creates.</summary>
    public async Task ConstrainQueryIdsAsync()
    {
        _ = await RunAsync(
            "CREATE CONSTRAINT pureql_query_id IF NOT EXISTS "
                + "FOR (q:Query) REQUIRE q.id IS UNIQUE"
        );
    }

    /// <summary>
    /// The shape of the graph stored under <paramref name="id"/>: its node and
    /// relationship counts, its record labels and relationship types, and the root's
    /// record label and id.
    /// </summary>
    public async Task<GraphShape> ShapeAsync(Guid id)
    {
        IRecord record = (
            await RunAsync(
                """
                MATCH (n:Node {queryId: $id})
                OPTIONAL MATCH (n)-[r]->()
                WITH n, collect(type(r)) AS types
                WITH
                  collect(n) AS nodes,
                  collect([l IN labels(n) WHERE NOT l IN ['Node', 'Query']][0]) AS labels,
                  reduce(acc = [], t IN collect(types) | acc + t) AS types
                RETURN
                  size(nodes) AS nodes,
                  size(types) AS relationships,
                  labels,
                  types,
                  [n IN nodes WHERE n:Query |
                    [l IN labels(n) WHERE NOT l IN ['Node', 'Query']][0]] AS roots,
                  [n IN nodes WHERE n:Query | n.id] AS ids
                """,
                id
            )
        ).Single();
        return new GraphShape(
            record["nodes"].As<long>(),
            record["relationships"].As<long>(),
            Sorted(record["labels"].As<List<string>>()),
            Sorted(record["types"].As<List<string>>()),
            string.Join(",", record["roots"].As<List<string>>()),
            string.Join(",", record["ids"].As<List<string>>())
        );
    }

    public async Task<List<IRecord>> RunAsync(string cypher, Guid? id = null)
    {
        Dictionary<string, object> parameters = [];
        if (id is Guid value)
        {
            parameters["id"] = value.ToString("D");
        }

        await using IAsyncSession session = Session();
        IResultCursor cursor = await session.RunAsync(cypher, parameters);
        return await cursor.ToListAsync();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _driver = GraphDatabase.Driver(_container.GetConnectionString(), AuthTokens.None);
    }

    public async Task DisposeAsync()
    {
        if (_driver is not null)
        {
            await _driver.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private static string Sorted(IEnumerable<string> values)
    {
        return string.Join(",", values.Order(StringComparer.Ordinal));
    }
}
