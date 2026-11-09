using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Graph.Neo4j.Insertion;
using PureQL.CSharp.Model.Samples.Queries.Dashboards;
using PureQL.CSharp.Model.Samples.Queries.Joins;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests;

[Collection(Neo4jServer.Name)]
public sealed class ReplacedQueryOfTests(Neo4jFixture neo4j)
{
    private readonly Neo4jFixture _neo4j = neo4j;

    private readonly INeo4jStoredQueries _store = new Neo4jStoredQueries(neo4j.Driver);

    [Fact]
    public async Task ReplacesTheStoredQuery()
    {
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);
        StoredQuery replacement = old with
        {
            Query = new ProductSalesPerformanceQuery().Value,
        };

        await new ReplacedQueryOf(_store, replacement).ApplyAsync();

        Assert.Equal(
            GraphShape.Of(replacement.Query, old.Id),
            await _neo4j.ShapeAsync(old.Id)
        );
    }

    [Fact]
    public async Task LeavesOtherQueriesUntouched()
    {
        StoredQuery other = await StoredAsync(new FullJoinQuery().Value);
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);

        await new ReplacedQueryOf(
            _store,
            old with
            {
                Query = new RightJoinQuery().Value,
            }
        ).ApplyAsync();

        Assert.Equal(
            GraphShape.Of(other.Query, other.Id),
            await _neo4j.ShapeAsync(other.Id)
        );
    }

    [Fact]
    public async Task InsertsAQueryThatIsNotStored()
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value);

        await new ReplacedQueryOf(_store, query).ApplyAsync();

        Assert.Equal(
            GraphShape.Of(query.Query, query.Id),
            await _neo4j.ShapeAsync(query.Id)
        );
    }

    [Fact]
    public async Task FailedInsertionKeepsTheOldGraph()
    {
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);

        _ = await Assert.ThrowsAsync<GraphMappingException>(() =>
            new ReplacedQueryOf(_store, old with { Query = default }).ApplyAsync()
        );

        Assert.Equal(GraphShape.Of(old.Query, old.Id), await _neo4j.ShapeAsync(old.Id));
    }

    [Fact]
    public async Task CancelledTokenKeepsTheOldGraph()
    {
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ReplacedQueryOf(
                _store,
                old with
                {
                    Query = new RightJoinQuery().Value,
                }
            ).ApplyAsync(new CancellationToken(true))
        );

        Assert.Equal(GraphShape.Of(old.Query, old.Id), await _neo4j.ShapeAsync(old.Id));
    }

    [Fact]
    public async Task UsesTheDatabaseOfTheStore()
    {
        StoredQuery old = await StoredAsync(new InnerJoinQuery().Value);

        _ = await Assert.ThrowsAsync<FatalDiscoveryException>(() =>
            new ReplacedQueryOf(
                new Neo4jStoredQueries(_neo4j.Driver, "missing"),
                old with
                {
                    Query = new RightJoinQuery().Value,
                }
            ).ApplyAsync()
        );

        Assert.Equal(GraphShape.Of(old.Query, old.Id), await _neo4j.ShapeAsync(old.Id));
    }

    [Fact]
    public async Task NullStoreOrQueryThrows()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new ReplacedQueryOf(
                null!,
                new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value)
            ).ApplyAsync()
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new ReplacedQueryOf(_store, null!).ApplyAsync()
        );
    }

    private async Task<StoredQuery> StoredAsync(PureQLQuery query)
    {
        StoredQuery stored = new StoredQuery(Guid.NewGuid(), query);
        await new InsertedQueryOf(_store, stored).ApplyAsync();
        Assert.Equal(GraphShape.Of(query, stored.Id), await _neo4j.ShapeAsync(stored.Id));
        return stored;
    }
}
