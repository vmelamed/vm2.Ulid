# Universaly Unique Lexicographically Sortable Identifier (ULID) for .NET

## Overview

A small, fast, and spec-compliant ULID (Universally Unique Lexicographically Sortable Identifier) implementation for .NET.
ULIDs combine a 48-bit timestamp (milliseconds since Unix epoch) with 80 bits of randomness, producing compact 128-bit
identifiers that are lexicographically sortable by creation time.

This repository exposes a `vm2.UlidType.Ulid` value type and an `UlidFactory` for stable, monotonic generation.

It follows closely the specification [ULID specification](https://github.com/ulid/spec).

## Comparison of ULID vs UUID (`System.Guid`)

The universaly unique lexicographically sortable identifiers (ULIDs) offer some advantages over traditional universally unique
identifiers (UUIDs or GUIDs) in certain scenarios:
- **Lexicographical Sorting**: ULIDs are designed to be lexicographically sortable, which is beneficial for **database indexing**
  and retrieval
- **Timestamp Component**: the most significant six bytes of the ULIDs are a timestamp component, allowing for **chronological
  ordering** of the identifiers
- **Change monotonically**: this can improve performance in certain database scenarios and cause **less fragmentation**. For
  identifiers generated in quick succession (within the same millisecond) the random component monotonically increases
- **Compact Representation**: the canonical representation of ULIDs is more compact compared to UUIDs: they are represented as
  a 26-character, case-insensitive string (Base32), while UUIDs are typically represented as a 36-character string, divided into
  five groups separated by hyphens, following this pattern: 8-4-4-4-12
- ULIDs are **binary-compatible with UUIDs**, allowing for easy integration in systems that already use UUIDs

## Get the code

Clone this repository:

```bash
  git clone https://github.com/vmelamed/vm2.git
  cd vm2
```

## Install the package (NuGet)

If a NuGet package is published for this project you can add it with the dotnet CLI:

```bash
  dotnet add vm2.UlidType.Ulid package UlidType
```

- Or from Visual Studio use the __Package Manager Console__:
  - Install-Package vm2.UlidType.Ulid

Build from source
- Command line:
  - dotnet build src/UlidType/UlidType.csproj
- Visual Studio:
  - Open the solution and choose __Build Solution__ (or use __Rebuild__ as needed).

Run the examples
- Create a small console project that references this library and run it, or use the example layout below:
  - dotnet new console -o src/UlidType.Example
  - dotnet add src/UlidType.Example/src/UlidType.Example.csproj reference src/UlidType/UlidType.csproj
  - dotnet run --project src/UlidType.Example

Packaging
- To create a NuGet package:
  - dotnet pack src/UlidType/UlidType.csproj -c Release

API usage examples
- Minimal example that demonstrates generation, formatting, parsing and introspection.