# Changelog

All notable changes to `PureQL.CSharp.Model.Graph.Neo4j.Updating` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.0.2.0] - 2026-11-11

### Added

- Targets the .NET 11 GA SDK, which CI now installs by version.

### Changed

- Depends on `PureQL.CSharp.Model.Graph.Neo4j.Insertion` and
  `PureQL.CSharp.Model.Graph.Neo4j.Deletion` `0.1.0-preview.0.2.0`,
  `PureQL.CSharp.Model.Graph` and `PureQL.CSharp.Model.Graph.Neo4j.Abstractions`
  `0.1.0-preview.1.1.0`.

## [0.1.0-preview.0.1.0] - 2026-11-09

First release: replaces a PureQL query graph stored in Neo4j, keeping its id.

### Added

- **`ReplacedQuery`** — `sealed record ReplacedQuery(IAsyncQueryRunner Runner,
  StoredQuery Query)`: deletes the stored query and inserts its replacement
  under the same id on the same runner, by composing `DeletedQuery` and
  `InsertedQuery`. On a transaction both commit or neither does; the
  transaction boundary is the caller's.
- **`ReplacedQueryOf`** — `sealed record ReplacedQueryOf(INeo4jStoredQueries
  Store, StoredQuery Query)`: replaces in one write transaction against the
  store's database, so a failed insertion rolls the deletion back and the
  stored query stays as it was.
- Replacing an id that is not stored inserts the query.
- Depends on `PureQL.CSharp.Model.Graph.Neo4j.Insertion` `0.1.0-preview.0.1.0`,
  `PureQL.CSharp.Model.Graph.Neo4j.Deletion` `0.1.0-preview.0.1.0`,
  `PureQL.CSharp.Model.Graph` `0.1.0-preview.1.0.0`,
  `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.1.0.0` and
  `Neo4j.Driver` `6.5.1`; targets .NET 11 and is AOT-compatible.
