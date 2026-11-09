using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Graph.Neo4j.Deletion;
using PureQL.CSharp.Model.Graph.Neo4j.Insertion;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating;

/// <summary>
/// A stored query replaced on a runner the caller owns: the graph stored under its id is
/// deleted and the graph of the new query is written under the same id.
/// </summary>
/// <param name="Runner">
/// The runner executing both statements. Pass a transaction so that the deletion and the
/// insertion commit together or not at all: the transaction boundary is the caller's.
/// On a session each statement commits on its own, and a failed insertion leaves the
/// query deleted.
/// </param>
/// <param name="Query">The new query, with the id of the query it replaces.</param>
/// <remarks>
/// Runs <see cref="DeletedQuery"/> for <see cref="StoredQuery.Id"/> and then
/// <see cref="InsertedQuery"/> for <see cref="Query"/>, on <see cref="Runner"/>, in that
/// order. Replacing an id that is not stored simply inserts the query, since deleting
/// what is not stored is a no-op.
/// </remarks>
public sealed record ReplacedQuery(IAsyncQueryRunner Runner, StoredQuery Query)
{
    /// <summary>Deletes the stored graph and writes the new one.</summary>
    /// <param name="cancellationToken">
    /// Checked before each statement is sent; the driver cannot cancel a running
    /// statement.
    /// </param>
    /// <returns>A task that completes once both statements have run.</returns>
    /// <exception cref="ClientException">The server rejects a statement.</exception>
    /// <exception cref="GraphMappingException">
    /// The query holds a default union or a null required value.
    /// </exception>
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(Runner);
        ArgumentNullException.ThrowIfNull(Query);
        await new DeletedQuery(Runner, Query.Id).ApplyAsync(cancellationToken);
        await new InsertedQuery(Runner, Query).ApplyAsync(cancellationToken);
    }
}
