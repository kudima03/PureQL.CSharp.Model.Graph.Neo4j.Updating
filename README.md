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

## Installation

```
dotnet add package PureQL.CSharp.Model.Graph.Neo4j.Updating
```
