using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating;

/// <summary>
/// A stored query replaced in the Neo4j database of <see cref="Store"/> in a transaction
/// of its own: the graph stored under its id is deleted and the graph of the new query is
/// written under the same id, both or neither.
/// </summary>
/// <param name="Store">The database holding the query.</param>
/// <param name="Query">The new query, with the id of the query it replaces.</param>
/// <remarks>
/// Opens a session on the store's <see cref="INeo4jStoredQueries.Driver"/> against its
/// <see cref="INeo4jStoredQueries.Database"/> and runs <see cref="ReplacedQuery"/> in one
/// managed write transaction (<c>IAsyncSession.ExecuteWriteAsync</c>), which the driver
/// retries on transient errors. When the insertion fails, the transaction rolls back
/// and the stored query is left as it was. Replacing an id that is not stored simply
/// inserts the query. To replace together with other operations, run
/// <see cref="ReplacedQuery"/> on a transaction of your own instead.
/// </remarks>
public sealed record ReplacedQueryOf(INeo4jStoredQueries Store, StoredQuery Query)
{
    /// <summary>Replaces the query and commits.</summary>
    /// <param name="cancellationToken">
    /// Checked before the session is opened and before each statement is sent; the
    /// driver cannot cancel a running transaction.
    /// </param>
    /// <returns>A task that completes once the transaction has committed.</returns>
    /// <exception cref="ClientException">
    /// The server rejects a statement. Nothing is changed.
    /// </exception>
    /// <exception cref="GraphMappingException">
    /// The query holds a default union or a null required value. Nothing is changed.
    /// </exception>
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(Store);
        ArgumentNullException.ThrowIfNull(Query);
        cancellationToken.ThrowIfCancellationRequested();
        await using IAsyncSession session = Store.Driver.AsyncSession(builder =>
            builder.WithDatabase(Store.Database)
        );
        await session.ExecuteWriteAsync(runner =>
            new ReplacedQuery(runner, Query).ApplyAsync(cancellationToken)
        );
    }
}
