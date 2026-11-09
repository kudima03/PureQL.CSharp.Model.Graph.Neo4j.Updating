# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory. The SDK is pinned by `global.json` (.NET 11, for the model's C# 15 unions); where the system SDK is older, use `~/.dotnet/dotnet` or put it first on `PATH` with `DOTNET_ROOT=$HOME/.dotnet`.

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes          # check code style (CI enforces this)
dotnet format                              # auto-fix code style
dotnet test --no-build --verbosity normal  # run tests
dotnet csharpier check .                   # check formatting (CI enforces this)
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`.

## Architecture

This is the **updating package** of the `PureQL.CSharp.Model.Graph` family: it replaces a stored query graph in Neo4j, keeping its id. It composes `PureQL.CSharp.Model.Graph.Neo4j.Deletion` `0.1.0-preview.0.1.0` and `PureQL.CSharp.Model.Graph.Neo4j.Insertion` `0.1.0-preview.0.1.0`, and references `PureQL.CSharp.Model.Graph` `0.1.0-preview.1.0.0`, `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.1.0.0` and `Neo4j.Driver` `6.5.1` explicitly, all by published NuGet version. Namespace `PureQL.CSharp.Model.Graph.Neo4j.Updating`, one public type per file:

- `ReplacedQuery` — `sealed record (IAsyncQueryRunner Runner, StoredQuery Query)`; `ApplyAsync` runs `DeletedQuery(Runner, Query.Id)` and then `InsertedQuery(Runner, Query)` on the same runner. On a transaction both commit or neither does; the transaction boundary is the caller's (on a session each statement commits on its own). Replacing an id that is not stored simply inserts, since the deletion is then a no-op; the `Query.id` uniqueness constraint is never hit because the old root is deleted first.
- `ReplacedQueryOf` — `sealed record (INeo4jStoredQueries Store, StoredQuery Query)`; `ApplyAsync` opens a session on `Store.Driver` for `Store.Database` and runs `ReplacedQuery` inside one `ExecuteWriteAsync`, so a failed insertion rolls the deletion back.

The driver's `RunAsync` and `ExecuteWriteAsync` take no cancellation token, so `ApplyAsync` checks its token before each statement. Exceptions (`GraphMappingException`, the driver's `ClientException`) propagate unwrapped.

The enclosing namespace `PureQL.CSharp.Model` has its own `Query` (a subquery), and `Neo4j` inside this namespace means `PureQL.CSharp.Model.Graph.Neo4j`: write the driver's query type as `global::Neo4j.Driver.Query` (or alias it).

**Test project:** `PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests` targets `net11.0` and uses xunit with coverlet. `ReplacedQueryTests` runs `ReplacedQuery` on `RecordingRunner`, a fake `IAsyncQueryRunner` that records the statements and the order their results are consumed. The integration tests share one `neo4j:5-community` container (`Neo4jFixture`, `Testcontainers.Neo4j`, random host port, no authentication; Docker must be running) through the `Neo4jServer` collection, seed with the published `InsertedQueryOf` under fresh ids, and never clean up. They compare a `GraphShape` (node and relationship counts, sorted record labels and relationship types, root label and id) read per `queryId` with the one `QueryGraphs.Of` gives: `ReplacedQueryOnServerTests` replaces a stored query with each of the 86 `PureQL.CSharp.Model.Samples` queries, and checks other queries are untouched, a missing id is inserted, the `pureql_query_id` constraint does not interfere, a caller's transaction commits or rolls back both statements, and a failed insertion (a default `PureQLQuery`) rolls back the deletion; `ReplacedQueryOfTests` runs the same cases end to end, plus cancellation, the store's database and null arguments.

**Target framework:** net11.0 only, as the model. `IsAotCompatible` is `true`.

**Package validation:** enabled, without a baseline until the first release; the `nuget-publish` skill sets `PackageValidationBaselineVersion` after each release, and intentional breaks go to `CompatibilitySuppressions.xml` next to the csproj.

**Mutation testing:** the Stryker step of CI is commented out.

**Publishing:** triggered by pushing a semver tag (pattern `*.*.*`). The tag becomes the `PackageVersion`. The workflow publishes to both GitHub Packages and NuGet.org.

## Code Style

Enforced via `.editorconfig` and `dotnet format --verify-no-changes` in CI:

- No `var` — always use explicit types
- No expression-bodied methods or constructors — use block bodies
- Expression-bodied properties and accessors are required
- File-scoped namespace declarations (`namespace Foo.Bar;`)
- Private fields prefixed with `_` in camelCase (e.g., `_options`)
- Braces always required, even for single-statement blocks
- Max line length: 90 characters
- Allman brace style — opening brace on its own line for all blocks
- `System.*` using directives sorted first, no blank line between using groups
- Parentheses required in arithmetic and relational binary operators for clarity

## Commit Messages

Do not mention Claude or AI assistance in commit messages.
