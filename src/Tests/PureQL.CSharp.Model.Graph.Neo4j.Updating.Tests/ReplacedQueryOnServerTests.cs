using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Graph.Neo4j.Insertion;
using PureQL.CSharp.Model.Samples.Queries.Joins;
using PureQL.CSharp.Model.Samples.Queries.Subqueries;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests;

[Collection(Neo4jServer.Name)]
public sealed class ReplacedQueryOnServerTests(Neo4jFixture neo4j)
{
    private readonly Neo4jFixture _neo4j = neo4j;

    [Fact]
    public void EverySampleIsLoaded()
    {
        Assert.Equal(151, QuerySamples.All.Count);
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task LeavesExactlyTheGraphOfTheNewQuery(string name)
    {
        StoredQuery old = await StoredAsync(new InlineStatusRowsQuery().Value);
        StoredQuery replacement = old with { Query = QuerySamples.All[name] };

        await ReplaceAsync(replacement);

        Assert.Equal(
            GraphShape.Of(replacement.Query, old.Id),
            await _neo4j.ShapeAsync(old.Id)
        );
        Assert.Equal(
            PureQLJson.Serialize(replacement.Query),
            PureQLJson.Serialize((await StoredGraph.ReadAsync(_neo4j, old.Id)).Query())
        );
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task LeavesNothingOfTheOldQuery(string name)
    {
        StoredQuery old = await StoredAsync(QuerySamples.All[name]);
        StoredQuery replacement = old with { Query = new InnerJoinQuery().Value };

        await ReplaceAsync(replacement);

        Assert.Equal(
            GraphShape.Of(replacement.Query, old.Id),
            await _neo4j.ShapeAsync(old.Id)
        );
        Assert.Equal(
            PureQLJson.Serialize(replacement.Query),
            PureQLJson.Serialize((await StoredGraph.ReadAsync(_neo4j, old.Id)).Query())
        );
    }

    [Fact]
    public async Task ReplacesTheListNodesOfInlineRows()
    {
        StoredQuery old = await StoredAsync(new InlineStatusRowsQuery().Value);
        long lists = ListNodes(QueryGraphs.Of(old.Query, old.Id));
        Assert.True(lists > 0);
        Assert.Equal(lists, await ListNodesAsync(old.Id));

        await ReplaceAsync(old);
        long replaced = await ListNodesAsync(old.Id);
        await ReplaceAsync(old with { Query = new InnerJoinQuery().Value });

        Assert.Equal(lists, replaced);
        Assert.Equal(0, await ListNodesAsync(old.Id));
    }

    [Fact]
    public async Task LeavesOtherQueriesUntouched()
    {
        StoredQuery other = await StoredAsync(new FullJoinQuery().Value);
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);

        await ReplaceAsync(old with { Query = new RightJoinQuery().Value });

        Assert.Equal(
            GraphShape.Of(other.Query, other.Id),
            await _neo4j.ShapeAsync(other.Id)
        );
    }

    [Fact]
    public async Task InsertsAQueryThatIsNotStored()
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value);

        await ReplaceAsync(query);

        Assert.Equal(
            GraphShape.Of(query.Query, query.Id),
            await _neo4j.ShapeAsync(query.Id)
        );
    }

    [Fact]
    public async Task ReplacesUnderTheQueryIdUniquenessConstraint()
    {
        await _neo4j.ConstrainQueryIdsAsync();
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);
        StoredQuery replacement = old with { Query = new RightJoinQuery().Value };

        await ReplaceAsync(replacement);

        Assert.Equal(
            GraphShape.Of(replacement.Query, old.Id),
            await _neo4j.ShapeAsync(old.Id)
        );
    }

    [Fact]
    public async Task CommitsWithTheCallersTransaction()
    {
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);
        StoredQuery replacement = old with { Query = new RightJoinQuery().Value };
        await using IAsyncSession session = _neo4j.Session();
        await using IAsyncTransaction transaction = await session.BeginTransactionAsync();

        await new ReplacedQuery(transaction, replacement).ApplyAsync();
        GraphShape uncommitted = await _neo4j.ShapeAsync(old.Id);
        await transaction.CommitAsync();

        Assert.Equal(GraphShape.Of(old.Query, old.Id), uncommitted);
        Assert.Equal(
            GraphShape.Of(replacement.Query, old.Id),
            await _neo4j.ShapeAsync(old.Id)
        );
    }

    [Fact]
    public async Task RollbackKeepsTheOldGraph()
    {
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);
        await using IAsyncSession session = _neo4j.Session();
        await using IAsyncTransaction transaction = await session.BeginTransactionAsync();

        await new ReplacedQuery(
            transaction,
            old with
            {
                Query = new RightJoinQuery().Value,
            }
        ).ApplyAsync();
        await transaction.RollbackAsync();

        Assert.Equal(GraphShape.Of(old.Query, old.Id), await _neo4j.ShapeAsync(old.Id));
    }

    [Fact]
    public async Task FailedInsertionRollsBackTheDeletionInTheCallersTransaction()
    {
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);
        await using IAsyncSession session = _neo4j.Session();

        _ = await Assert.ThrowsAsync<GraphMappingException>(() =>
            session.ExecuteWriteAsync(transaction =>
                new ReplacedQuery(transaction, old with { Query = default }).ApplyAsync()
            )
        );

        Assert.Equal(GraphShape.Of(old.Query, old.Id), await _neo4j.ShapeAsync(old.Id));
    }

    public static TheoryData<string> Samples()
    {
        return QuerySamples.Names();
    }

    private async Task<StoredQuery> StoredAsync(PureQLQuery query)
    {
        StoredQuery stored = new StoredQuery(Guid.NewGuid(), query);
        await new InsertedQueryOf(
            new Neo4jStoredQueries(_neo4j.Driver),
            stored
        ).ApplyAsync();
        Assert.Equal(GraphShape.Of(query, stored.Id), await _neo4j.ShapeAsync(stored.Id));
        return stored;
    }

    private static long ListNodes(QueryGraph graph)
    {
        return graph.Nodes.Count(node => node.Labels.Contains(GraphLabels.List));
    }

    private async Task<long> ListNodesAsync(Guid id)
    {
        return (
            await _neo4j.RunAsync(
                "MATCH (n:List {queryId: $id}) RETURN count(n) AS lists",
                id
            )
        )
            .Single()["lists"]
            .As<long>();
    }

    private async Task ReplaceAsync(StoredQuery query)
    {
        await using IAsyncSession session = _neo4j.Session();
        await session.ExecuteWriteAsync(transaction =>
            new ReplacedQuery(transaction, query).ApplyAsync()
        );
    }
}
