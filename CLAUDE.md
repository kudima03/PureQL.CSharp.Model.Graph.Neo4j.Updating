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

- `ReplacedQuery` — `sealed record (IAsyncQueryRunner Runner, StoredQuery Query)`; `ApplyAsync` runs `DeletedQuery(Runner, Query.Id)` and then `InsertedQuery(Runner, Query)` on the same runner.

The enclosing namespace `PureQL.CSharp.Model` has its own `Query` (a subquery), and `Neo4j` inside this namespace means `PureQL.CSharp.Model.Graph.Neo4j`: write the driver's query type as `global::Neo4j.Driver.Query` (or alias it).

**Test project:** `PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests` targets `net11.0` and uses xunit with coverlet. `ReplacedQueryTests` runs `ReplacedQuery` on `RecordingRunner`, a fake `IAsyncQueryRunner` that records the statements and the order their results are consumed.

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
