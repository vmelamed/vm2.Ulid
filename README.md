# Universaly Unique Lexicographically Sortable Identifier (ULID) for .NET

## Overview

A small, fast, and spec-compliant [ULID](https://github.com/ulid/spec) (Universally Unique Lexicographically Sortable Identifier)
implementation for .NET.

ULIDs combine a 48-bit timestamp (milliseconds since Unix epoch) with 80 bits of randomness, producing compact 128-bit
identifiers that are lexicographically sortable by creation time.

This repository exposes a `vm2.Ulid` value type and an `vm2.UlidFactory` for stable, monotonic generation.

## Short Comparison of ULID vs UUID (`System.Guid`)

The universaly unique lexicographically sortable identifiers (ULIDs) offer some advantages over the traditional universally unique
identifiers (UUIDs or GUIDs) in certain scenarios:

- **Lexicographical Sorting**: ULIDs are designed to be lexicographically sortable, which may be beneficial for **database indexing**
  and retrieval
- **Timestamp Component**: the most significant six bytes of the ULIDs are a timestamp component, allowing for **chronological
  ordering** of the identifiers
- **Change monotonically**: this can improve performance in certain database scenarios and cause **less fragmentation**. For
  identifiers generated in quick succession (within the same millisecond) the random component monotonically increments
- **Compact Representation**: the canonical representation of ULIDs is more compact compared to UUIDs: they are represented as
  a 26-character, case-insensitive string (Base32), while UUIDs are typically represented as a 36 hexadecimal characters, divided
  into five groups separated by hyphens, following the pattern: 8-4-4-4-12
- ULIDs are **binary-compatible with UUIDs**, allowing for easy integration in systems that already use UUIDs

## Basic Usage

```csharp
using vm2;

// Recommended: create and reuse more than one UlidFactory-ies, e.g. one per DB table or entity type which require ULIDs.
// This ensures that ULIDs generated in different contexts have their own monotonicity guarantees.

UlidFactory factory = new UlidFactory();

Ulid ulid1 = factory.NewUlid();
Ulid ulid2 = factory.NewUlid();

// Using the default internal factory ensures thread-safety and monotonicity within the same millisecond for ULIDs generated in
// different contexts of the app or service.

Ulid ulid = Ulid.NewUlid();

Debug.Assert(ulid1 != ulid2);                           // uniqueness
Debug.Assert(ulid < ulid2);                             // comparable

var ulid1String = ulid1.String();                       // get the canonical string representation
var ulid2String = ulid1.String();

Debug.Assert(ulid1String != ulid2String);               // ULID strings are unique
Debug.Assert(ulid1String < ulid2String);                // ULID strings are lexicographically sortable
Debug.Assert(ulid1String.Length == 26);                 // ULID string representation is 26 characters long

Debug.Assert(ulid1 <= ulid2);
Debug.Assert(ulid1.Timestamp < ulid2.Timestamp ||       // ULIDs are time-sortable and the timestamp can be extracted
             ulid1.RandomBytes != ulid2.RandomBytes);   // if generated in the same millisecond, the random part is guaranteed to be different

Debug.Assert(ulid1.RandomBytes.ToArray().Length == 10); // ULID has 10 bytes of randomness

Debug.Assert(ulid1.Bytes.ToArray().Length == 16);       // ULID is a 16-byte (128-bit) value

var ulidGuid  = ulid1.ToGuid();                         // ULID can be converted to Guid
var ulidFromGuid = new Ulid(ulidGuid);                  // ULID can be created from Guid

var ulidUtf8String = Encoding.UTF8.GetBytes(ulid1String);

Ulid.TryParse(ulidString, out var ulidCopy1);           // ULID can be parsed from UTF-16 string representation (26 UTF-16 characters)
Ulid.TryParse(ulidUtf8String, out var ulidCopy2);       // ULID can be parsed from its UTF-8 string representation (26 UTF-8 characters/bytes)

Debug.Assert(ulid1 == ulidCopy1 &&
             ulid1 == ulidCopy2);                       // Parsed ULIDs are equal to the original
```

## Why do I need `UlidFactory`?

One of the requirements for the ULIDs is that they increase monotonically within the same millisecond. This means that if you
generate multiple ULIDs in the same millisecond, each subsequent ULID must be greater than the previous one by one in the least
significant byte(s). To ensure that, there must be a ULID generating object (factory) that keeps track of the timestamp and the
last generated random bytes. If during generation of a ULID the factory finds out that the current timestamp is the same as the
timestamp of the previous generation within to the millisecond, the factory must increment the previous random part and use it,
rather than generating new randomness.

The `UlidFactory` class encapsulates this logic, providing a simple interface for generating ULIDs that meet this requirement.

It is prudent to create and reuse more than one `UlidFactory` instances, e.g. one per DB table or entity type which require
ULIDs. But you can also use the static `Ulid.NewUlid()` method which uses an internal instance of a factory. The ULID factory(s)
are thread-safe and ensures monotonicity of ULIDs generated in different contexts of an application or service.

## Get the code

Clone this repository:

  ```bash
  git clone https://github.com/vmelamed/vm2.Ulid.git
  cd vm2
  ```

## Install the package (NuGet)

If a NuGet package is published for this project you can add it

- with the dotnet CLI:

  ```bash
  dotnet add vm2.Ulid package vm2.Ulid
  ```

- Or from Visual Studio use the __Package Manager Console__:

  ```powershell
  Install-Package vm2.Ulid
  ```

Build from source
- Command line:

  ```bash
  dotnet build src/UlidType/UlidType.csproj
  ```

- Visual Studio:
  - Open the solution and choose __Build Solution__ (or use __Rebuild__ as needed).

Run the examples
- Create a small console project that references this library and run it, or use the example layout below:

  ```bash
  dotnet new console -o src/UlidType.Example
  dotnet add src/UlidType.Example/src/UlidType.Example.csproj reference src/UlidType/UlidType.csproj
  dotnet run --project src/UlidType.Example
  ```

Packaging
- To create a NuGet package:
  ```bash
  dotnet pack src/UlidType/UlidType.csproj -c Release
  ```

API usage examples
- Minimal example that demonstrates generation, formatting, parsing and introspection.

## Performance

You can build and run the benchmark tests in release mode with:

```bash
vm2.Ulid/benchmarks/UlidType.Benchmarks/bin/Release/net9.0/UlidType.Benchmarks.exe --filter * --memory --artifacts ..\..\..\BenchmarkDotNet.Artifacts
```

Here are some benchmark results with similar Guid functions as baselines from a run on a Windows 10 machine with:

- Processor: Intel(R) Core(TM) Ultra 9 185H, 2500 Mhz, 16 Core(s), 22 Logical Processor(s)
- Installed Physical Memory:	64.0 GB
- Available Physical Memory:	32.2 GB

| Method               | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|:-------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Guid.NewGuid         | 45.70 ns | 0.783 ns | 2.308 ns |  1.00 |    0.07 |      - |         - |          NA |
| UlidFactory.NewUlid  | 44.16 ns | 0.143 ns | 0.409 ns |  0.97 |    0.05 | 0.0032 |      40 B |          NA |
| Ulid.NewUlid         | 46.20 ns | 0.555 ns | 1.637 ns |  1.01 |    0.06 | 0.0032 |      40 B |          NA |
|----------------------|----------|----------|----------|-------|---------|--------|-----------|-------------|
| Guid.Parse           | 17.07 ns | 0.045 ns | 0.117 ns |  1.00 |    0.01 |      - |         - |          NA |
| Ulid.ParseUtf8String | 54.35 ns | 0.946 ns | 2.761 ns |  3.18 |    0.16 | 0.0063 |      80 B |          NA |
| Ulid.ParseString     | 56.23 ns | 0.847 ns | 2.470 ns |  3.29 |    0.15 | 0.0063 |      80 B |          NA |
|----------------------|----------|----------|----------|-------|---------|--------|-----------|-------------|
| Guid.ToString        | 11.62 ns | 0.159 ns | 0.466 ns |  1.00 |    0.06 | 0.0076 |      96 B |        1.00 |
| Ulid.ToString        | 29.00 ns | 0.500 ns | 1.474 ns |  2.50 |    0.16 | 0.0063 |      80 B |        0.83 |

Legend:
  - Mean      : Arithmetic mean of all measurements
  - Error     : Half of 99.9% confidence interval
  - StdDev    : Standard deviation of all measurements
  - Ratio     : Mean of the ratio distribution ([Current]/[Baseline])
  - RatioSD   : Standard deviation of the ratio distribution ([Current]/[Baseline])
  - Gen0      : GC Generation 0 collects per 1000 operations
  - Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  - 1 ns      : 1 Nanosecond (0.000000001 sec)
