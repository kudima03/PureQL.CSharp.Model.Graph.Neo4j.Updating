using Neo4j.Driver;
using Neo4jQuery = Neo4j.Driver.Query;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests;

/// <summary>
/// A query runner that records the statements it is given and the order in which their
/// results are consumed, without a server.
/// </summary>
internal sealed class RecordingRunner : IAsyncQueryRunner
{
    private readonly List<string> _events = [];

    public IReadOnlyList<string> Events => _events;

    public List<Neo4jQuery> Statements { get; } = [];

    public Task<IResultCursor> RunAsync(Neo4jQuery query)
    {
        Statements.Add(query);
        _events.Add($"run {Statements.Count}");
        return Task.FromResult<IResultCursor>(
            new RecordingCursor(_events, Statements.Count)
        );
    }

    public Task<IResultCursor> RunAsync(string query)
    {
        return RunAsync(new Neo4jQuery(query));
    }

    public Task<IResultCursor> RunAsync(string query, object parameters)
    {
        return RunAsync(new Neo4jQuery(query, parameters));
    }

    public Task<IResultCursor> RunAsync(
        string query,
        IDictionary<string, object> parameters
    )
    {
        return RunAsync(new Neo4jQuery(query, parameters));
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public void Dispose() { }

    private sealed class RecordingCursor(List<string> events, int number) : IResultCursor
    {
        public IRecord Current => throw new InvalidOperationException("No records.");

        public bool IsOpen => false;

        public Task<IResultSummary> ConsumeAsync()
        {
            events.Add($"consume {number}");
            return Task.FromResult<IResultSummary>(null!);
        }

        public Task<bool> FetchAsync()
        {
            return Task.FromResult(false);
        }

        public Task<string[]> KeysAsync()
        {
            return Task.FromResult(Array.Empty<string>());
        }

        public Task<IRecord> PeekAsync()
        {
            return Task.FromResult<IRecord>(null!);
        }

        public IAsyncEnumerator<IRecord> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        )
        {
            return AsyncEnumerable.Empty<IRecord>().GetAsyncEnumerator(cancellationToken);
        }
    }
}
