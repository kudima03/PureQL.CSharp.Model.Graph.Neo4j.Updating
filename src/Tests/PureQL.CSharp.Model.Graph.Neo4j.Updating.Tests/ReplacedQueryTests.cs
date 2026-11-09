using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Graph.Neo4j.Deletion;
using PureQL.CSharp.Model.Graph.Neo4j.Insertion;
using PureQL.CSharp.Model.Samples.Queries.Joins;
using Neo4jQuery = Neo4j.Driver.Query;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests;

public sealed class ReplacedQueryTests
{
    [Fact]
    public async Task RunsTheDeletionThenTheInsertionOnTheRunner()
    {
        RecordingRunner runner = new RecordingRunner();
        StoredQuery query = new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value);
        Neo4jQuery deletion = new DeleteByIdStatement(query.Id).Value;
        Neo4jQuery insertion = new InsertStatement(
            QueryGraphs.Of(query.Query, query.Id)
        ).Value;

        await new ReplacedQuery(runner, query).ApplyAsync();

        Assert.Equal(
            [
                (deletion.Text, deletion.Parameters.Count),
                (insertion.Text, insertion.Parameters.Count),
            ],
            runner.Statements.Select(statement =>
                (statement.Text, statement.Parameters.Count)
            )
        );
        Assert.Equal(query.Id.ToString("D"), runner.Statements[0].Parameters["id"]);
    }

    [Fact]
    public async Task ConsumesTheDeletionBeforeRunningTheInsertion()
    {
        RecordingRunner runner = new RecordingRunner();

        await new ReplacedQuery(
            runner,
            new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value)
        ).ApplyAsync();

        Assert.Equal(["run 1", "consume 1", "run 2", "consume 2"], runner.Events);
    }

    [Fact]
    public async Task CancelledTokenRunsNothing()
    {
        RecordingRunner runner = new RecordingRunner();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ReplacedQuery(
                runner,
                new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value)
            ).ApplyAsync(new CancellationToken(true))
        );

        Assert.Empty(runner.Statements);
    }

    [Fact]
    public async Task NullRunnerOrQueryThrows()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new ReplacedQuery(
                null!,
                new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value)
            ).ApplyAsync()
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new ReplacedQuery(new RecordingRunner(), null!).ApplyAsync()
        );
    }
}
