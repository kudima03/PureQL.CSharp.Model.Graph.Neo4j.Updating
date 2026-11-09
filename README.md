# PureQL.CSharp.Model.Graph.Neo4j.Updating

Replaces a **PureQL** query stored in Neo4j, keeping its id.

[![.NET build & test](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Updating/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Updating/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Updating/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Updating/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/PureQL.CSharp.Model.Graph.Neo4j.Updating)](https://www.nuget.org/packages/PureQL.CSharp.Model.Graph.Neo4j.Updating)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

`PureQL.CSharp.Model.Graph.Neo4j.Updating` is the updating package of the `PureQL.CSharp.Model.Graph` family. It replaces the graph stored under a query's id with the graph of a new query by composing the [Deletion](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Deletion) and [Insertion](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion) packages on one runner.

## Public API

Namespace `PureQL.CSharp.Model.Graph.Neo4j.Updating`:

| Name | Description |
|------|-------------|
| `ReplacedQuery` | `sealed record ReplacedQuery(IAsyncQueryRunner Runner, StoredQuery Query)`; `ApplyAsync` deletes the stored query and writes the new one on a session or transaction you own |
| `ReplacedQueryOf` | `sealed record ReplacedQueryOf(INeo4jStoredQueries Store, StoredQuery Query)`; `ApplyAsync` opens a session on the store and replaces the query in one managed write transaction |

## Usage

```csharp
using Neo4j.Driver;
using PureQL.CSharp.Model;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Graph.Neo4j.Updating;

await using IDriver driver = GraphDatabase.Driver(
    "neo4j://localhost:7687",
    AuthTokens.Basic("neo4j", "password")
);
INeo4jStoredQueries store = new Neo4jStoredQueries(driver);
Guid id = /* the id of a stored query */;
PureQLQuery edited = /* the query that replaces it */;

// In a transaction of its own:
await new ReplacedQueryOf(store, new StoredQuery(id, edited)).ApplyAsync();

// Or together with other operations, in your transaction:
await using IAsyncSession session = driver.AsyncSession(
    builder => builder.WithDatabase(store.Database)
);
await session.ExecuteWriteAsync(
    async transaction =>
    {
        await new ReplacedQuery(transaction, new StoredQuery(id, edited)).ApplyAsync();
        // ...other operations on the same transaction
    }
);
```

## How a query is replaced

`ReplacedQuery` runs two statements on its runner, in order:

1. [`DeletedQuery`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Deletion)`(Runner, Query.Id)` — `MATCH (n:Node {queryId: $id}) DETACH DELETE n` removes every node of the stored graph and every relationship attached to them.
2. [`InsertedQuery`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion)`(Runner, Query)` — writes the graph of the new query under the same id in one statement.

The query keeps its id, so the `Query.id` uniqueness constraint (`pureql_query_id`, created by the Creation package) is never hit: the old root is deleted before the new one is written.

**Replacing an id that is not stored simply inserts the query**: deleting what is not stored is a no-op.

## Transactions

- `ReplacedQueryOf` runs both statements in one managed write transaction (`ExecuteWriteAsync`). If the insertion fails — the query cannot be mapped to a graph (`GraphMappingException`), the server rejects the statement (`ClientException`), or the token is cancelled — the transaction rolls back and the stored query is left as it was. The exception is not wrapped.
- With `ReplacedQuery`, **the transaction boundary is yours**. On an `IAsyncTransaction` both statements commit or roll back together with the rest of your work. On an `IAsyncSession` each statement commits on its own, so a failed insertion leaves the query deleted; pass a transaction unless that is what you want.

## Cancellation

`Neo4j.Driver` 6.x takes no cancellation token for a running statement, so `ApplyAsync` checks its token before each statement it sends (and `ReplacedQueryOf` also before it opens the session).

## Design Principles

- **Sealed records** with their dependencies in the constructor; no static services
- **Composable** — `ReplacedQuery` runs on any `IAsyncQueryRunner`, a session or a transaction, and is itself built from the Deletion and Insertion packages
- **AOT-compatible** — `IsAotCompatible=true`; no reflection

## Dependencies

- [`PureQL.CSharp.Model.Graph.Neo4j.Deletion`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Deletion) `0.1.0-preview.0.1.0` — `DeletedQuery`
- [`PureQL.CSharp.Model.Graph.Neo4j.Insertion`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion) `0.1.0-preview.0.1.0` — `InsertedQuery`
- [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) `0.1.0-preview.1.0.0` — `QueryGraphs`, `GraphMappingException`
- [`PureQL.CSharp.Model.Graph.Neo4j.Abstractions`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Abstractions) `0.1.0-preview.1.0.0` — `INeo4jStoredQueries`, `StoredQuery`
- [`Neo4j.Driver`](https://www.nuget.org/packages/Neo4j.Driver) `6.5.1`

## Target Frameworks

- .NET 11

## Installation

```
dotnet add package PureQL.CSharp.Model.Graph.Neo4j.Updating
```

## Related packages

- [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) — maps a `PureQLQuery` to a property graph and back, without a driver
- [`PureQL.CSharp.Model.Graph.Neo4j.Abstractions`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Abstractions) — the stored-queries contract the Neo4j packages share
- [`PureQL.CSharp.Model.Graph.Neo4j.Creation`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Creation) — constraints and indexes
- [`PureQL.CSharp.Model.Graph.Neo4j.Insertion`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion) — writes queries
- [`PureQL.CSharp.Model.Graph.Neo4j.Querying`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Querying) — reads queries back
- [`PureQL.CSharp.Model.Graph.Neo4j.Deletion`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Deletion) — deletes queries
- [`PureQL.CSharp.Model.Graph.Neo4j.Existence`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Existence) — checks the schema and queries exist
- [`PureQL.CSharp.Model.Graph.Neo4j.Destruction`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Destruction) — drops the schema and every stored query
- [`PureQL.CSharp.Model.Graph.Neo4j.Migration`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Migration) — migrates stored graphs between model versions
- [`PureQL.CSharp.Model.Serialization`](https://github.com/kudima03/PureQL.CSharp.Model.Serialization) — JSON for the same model
