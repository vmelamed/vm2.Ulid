# Universally Unique Lexicographically Sortable Identifier (ULID) for .NET

[![CI](https://github.com/vmelamed/vm2.Ulid/actions/workflows/CI.yaml/badge.svg?branch=main)](https://github.com/vmelamed/vm2.Ulid/actions/workflows/CI.yaml)
[![codecov](https://codecov.io/gh/vmelamed/vm2.Ulid/branch/main/graph/badge.svg?branch=main)](https://codecov.io/gh/vmelamed/vm2.Ulid)
[![Release](https://github.com/vmelamed/vm2.Ulid/actions/workflows/Release.yaml/badge.svg?branch=main)](https://github.com/vmelamed/vm2.Ulid/actions/workflows/Release.yaml)

[![NuGet Version](https://img.shields.io/nuget/v/vm2.Ulid)](https://www.nuget.org/packages/vm2.Ulid/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/vm2.Ulid.svg)](https://www.nuget.org/packages/vm2.Ulid/)
[![GitHub License](https://img.shields.io/github/license/vmelamed/vm2.Ulid)](https://github.com/vmelamed/vm2.Ulid/blob/main/LICENSE)

<!-- TOC tocDepth:2..5 chapterDepth:2..6 -->

- [Overview](#overview)
- [Short Comparison of ULID vs UUID (`System.Guid`)](#short-comparison-of-ulid-vs-uuid-systemguid)
- [Prerequisites](#prerequisites)
- [Install the package (NuGet)](#install-the-package-nuget)
- [Get the code](#get-the-code)
- [Build from the source code](#build-from-the-source-code)
- [Tests](#tests)
- [Benchmark Tests](#benchmark-tests)
- [Build and Run the Example](#build-and-run-the-example)
- [Basic Usage](#basic-usage)
- [Why do I need `UlidFactory`?](#why-do-i-need-ulidfactory)
  - [The `UlidFactory` in a distributed system](#the-ulidfactory-in-a-distributed-system)
- [Performance](#performance)
- [Related Packages](#related-packages)
- [License](#license)

<!-- /TOC -->
## Overview

A small, fast, and spec-compliant [ULID](https://github.com/ulid/spec) (Universally Unique Lexicographically Sortable Identifier)
implementation for .NET.

ULIDs combine a 48-bit timestamp (milliseconds since Unix epoch) with 80 bits of randomness, producing compact 128-bit
identifiers that are lexicographically sortable by creation time.

This package exposes a `vm2.Ulid` value type and an `vm2.UlidFactory` for stable, monotonic generation.

## Short Comparison of ULID vs UUID (`System.Guid`)

The universaly unique lexicographically sortable identifiers (ULIDs) offer some advantages over the traditional universally
unique identifiers (UUIDs or GUIDs) in certain scenarios:

- **Lexicographical Sorting**: ULIDs are designed to be lexicographically sortable, which may be beneficial for **database
  indexing** and retrieval
- **Timestamp Component**: the most significant six bytes of the ULIDs are a timestamp component, allowing for **chronological
  ordering** of the identifiers
- **Change monotonically**: this can improve performance in certain database scenarios and cause **less fragmentation**. For
  identifiers generated in quick succession (within the same millisecond) the random component monotonically increments
- **Compact Representation**: the canonical representation of ULIDs is more compact compared to UUIDs: they are represented as
  a 26-character, case-insensitive string (Crockford Base32), while UUIDs are typically represented as a 36 hexadecimal
  characters, divided into five groups separated by hyphens, following the pattern: 8-4-4-4-12.
- In the canonical, 26-character, string representation of ULIDs **the first 10 characters encode the timestamp**, providing a
  human-readable indication of the creation time; and the remaining 16 characters encode the random component. Whereas in the
  UUID string representation the meaning of the groups depends on the version, and typically **does not provide a
  straightforward way to extract the creation time**, therefore ULIDs can be used to **generate sortable identifiers** and are
  not sortable chronologically. The most popular UUID version 4 all parts are randomly generated and do not include a timestamp
  component at all.
- ULIDs are **binary-compatible with UUIDs** (both are 128-bit values), allowing for easy integration in systems that already
  use UUIDs

## Prerequisites

- .NET 10.0 or later

## Install the package (NuGet)

- Using the dotnet CLI:

  ```bash
  dotnet add vm2.Ulid package vm2.Ulid
  ```

- From Visual Studio **Package Manager Console**:

  ```powershell
  Install-Package vm2.Ulid
  ```

## Get the code

You can clone the the [GitHub repository](https://github.com/vm2/vm2.Ulid). The project is in the `src/UlidType` directory.

## Build from the source code

- Command line:

  ```bash
  dotnet build src/UlidType/UlidType.csproj
  ```

- Visual Studio:
  - Open the solution and choose **Build Solution** (or **Rebuild** as needed).

## Tests

The test project is located in the `test` directory. It is using the new MTP v2 with the xUnit framework v3.2.2, which may or
may not be compatible with your version of Visual Studio but the tests are buildable and runnable from the command line using
the `dotnet` CLI. Which, of course, also works from Visual Studio Code under various OS-es.

- Command line:

  ```bash
  dotnet test --project test/UlidType.Tests/UlidType.Tests.csproj
  ```

- The tests can also be run standalone after building the solution or the test project:

  - build the solution or the test project only:

    ```bash
    dotnet build # build the full solution or
    dotnet build test/UlidType.Tests/UlidType.Tests.csproj # the test project only
    ```

  - Run the tests standalone (Linux/macOS):

    ```bash
    test/UlidType.Tests/bin/Debug/net10.0/UlidType.Tests
    ```

  - Run the tests standalone (Windows):

    ```bash
    test/UlidType.Tests/bin/Debug/net10.0/UlidType.Tests.exe
    ```

## Benchmark Tests

The benchmark tests project is located in the `benchmarks` directory. It is using the BenchmarkDotNet v0.13.8 library for
benchmarking. The benchmarks are buildable and runnable from the command line using the `dotnet` CLI.

- Command line:

  ```bash
  dotnet run --project benchmarks/UlidType.Benchmarks/UlidType.Benchmarks.csproj -c Release
  ```

- The benchmarks can also be run standalone after building the benchmark project:

  - build the benchmark project only:

    ```bash
    dotnet build -c Release benchmarks/UlidType.Benchmarks/UlidType.Benchmarks.csproj
    ```

  - Run the benchmarks standalone (Linux/macOS):

    ```bash
    benchmarks/UlidType.Benchmarks/bin/Release/net10.0/UlidType.Benchmarks
    ```

  - Run the benchmarks standalone (Windows):

    ```bash
    benchmarks/UlidType.Benchmarks/bin/Release/net10.0/UlidType.Benchmarks.exe
    ```

## Build and Run the Example

The example project is located in the `examples/GenerateUlids` directory. It is a simple console application that demonstrates
the basic usage of the `vm2.Ulid` library. The example is buildable and runnable from the command line using the `dotnet` CLI.

- Command line:

  ```bash
  dotnet run --project examples/GenerateUlids/GenerateUlids.csproj
  ```

- The example can also be run standalone after building the example project:

  - build the example project only:

    ```bash
    dotnet build examples/GenerateUlids/GenerateUlids.csproj
    ```

  - Run the example standalone (Linux/macOS):

    ```bash
    examples/GenerateUlids/bin/Debug/net10.0/GenerateUlids
    ```

  - Run the example standalone (Windows):

    ```bash
    examples/GenerateUlids/bin/Debug/net10.0/GenerateUlids.exe
    ```

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
Debug.Assert(ulid1 < ulid2);                            // comparable
Debug.Assert(ulid  > ulid2);                            // comparable

var ulid1String = ulid1.String();                       // get the ULID canonical string representation
var ulid2String = ulid1.String();

Debug.Assert(ulid1String != ulid2String);               // ULID strings are unique
Debug.Assert(ulid1String < ulid2String);                // ULID strings are lexicographically sortable
Debug.Assert(ulid1String.Length == 26);                 // ULID string representation is 26 characters long

Debug.Assert(ulid1 <= ulid2);
Debug.Assert(ulid1.Timestamp < ulid2.Timestamp ||       // ULIDs are time-sortable and the timestamp can be extracted
             ulid1.Timestamp == ulid2.Timestamp &&      // if generated in the same millisecond
             ulid1.RandomBytes != ulid2.RandomBytes);   // the random parts are guaranteed to be different

Debug.Assert(ulid1.RandomBytes.Length == 10);           // ULID has 10 bytes of randomness

Debug.Assert(ulid1.Bytes.Length == 16);                 // ULID is a 16-byte (128-bit) value

var ulidGuid  = ulid1.ToGuid();                         // ULID can be converted to Guid
var ulidFromGuid = new Ulid(ulidGuid);                  // ULID can be created from Guid

var ulidUtf8String = Encoding.UTF8.GetBytes(ulid1String);

Ulid.TryParse(ulid1String, out var ulidCopy1);          // parse ULID from UTF-16 string (26 UTF-16 characters)
Ulid.TryParse(ulidUtf8String, out var ulidCopy2);       // parse ULID from its UTF-8 string (26 UTF-8 characters/bytes)

Debug.Assert(ulid1 == ulidCopy1 &&                      // Parsed ULIDs are equal to the original
             ulid1 == ulidCopy2);
```

## Why do I need `UlidFactory`?

One of the requirements for the ULIDs is that they increase monotonically within the same millisecond. This means that if you
generate multiple ULIDs in the same millisecond, each subsequent ULID must be greater than the previous one by one in the least
significant byte(s). To ensure conformance to this requirement, there must be a ULID generating object (factory) that keeps
track of the timestamp and the last generated random bytes. If during generation of a ULID the factory finds out that the
current timestamp is the same as the timestamp of the previous generation to the millisecond, the factory must increment the
previous random part and use this new value, rather than generating new random values.

The `UlidFactory` class encapsulates this logic, providing a simple interface for generating ULIDs that meet this requirement.

It is prudent to create and reuse more than one `UlidFactory` instances, e.g. one per DB table or entity type which require
ULIDs. The ULID factory(s) are thread-safe and ensure monotonicity of the generated ULIDs in different contexts of an
application or a service.

By default the `UlidFactory` uses a cryptographic random number generator (`vm2.UlidRandomProviders.CryptoRandom`), which is
suitable for most applications. If you need a different source of randomness (e.g. for testing or performance reasons) or you
are concerned about the source of entropy, you can explicitly specify that the factory should use the pseudo-random number
generator `vm2.UlidRandomProviders.PseudoRandom`. You can also provide your own, thread-safe implementation of
`vm2.IRandomNumberGenerator` to the factory.

In contrast, if you do not want to use the `vm2.UlidFactory` directly, you can use the static `vm2.Ulid.NewUlid()` method, which
uses an internal static factory instance with a cryptographic random number generator.

### The `UlidFactory` in a distributed system

In a distributed system, it is important to ensure that ULIDs generated on different nodes do not collide and maintain their
monotonicity. One way to achieve this is to have a separate `UlidFactory` instance on each node, each with its own unique
node identifier. This way, even if two nodes generate ULIDs at the same millisecond, the ULIDs will be different due to the
different node identifiers. However, this approach breaks the global monotonicity requirement across all nodes.

To maintain global monotonicity in a distributed system, one simple approach is to use a centralized service to generate ULIDs
for all nodes. This ensures that ULIDs are unique and monotonic across the entire system, but it introduces a single point of
failure and potential performance bottleneck. Another approach is to use a consensus algorithm to coordinate ULID generation
across nodes, but this adds complexity and overhead.

The choice of approach depends on the specific requirements and constraints of your distributed system. The problems outlined
above are just a the first ones that come in mind. In any case, it is important to carefully consider the trade-offs between
uniqueness, monotonicity, performance, and complexity when designing your ULID generation strategy in a distributed environment.

## Performance

Here are some benchmark results with similar Guid functions as baselines run on GitHub Actions:

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)

AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores .NET SDK 9.0.305

- [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
- DefaultJob : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

| Method               | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio | RandomProviderType |
|--------------------  |----------:|---------:|---------:|------:|-------:|----------:|------------:|------------------- |
| UlidFactory.NewUlid  |  56.12 ns | 0.085 ns | 0.071 ns |  0.09 | 0.0024 |      40 B |          NA | CryptoRandom       |
| Ulid.NewUlid         |  56.49 ns | 0.105 ns | 0.098 ns |  0.09 | 0.0024 |      40 B |          NA | CryptoRandom       |
| Guid.NewGuid[^1]     | 595.28 ns | 1.149 ns | 0.897 ns |  1.00 |      - |         - |          NA |                    |
|                      |           |          |          |       |        |           |             |                    |
| UlidFactory.NewUlid  |  55.99 ns | 0.106 ns | 0.094 ns |  0.09 | 0.0024 |      40 B |          NA | PseudoRandom       |
| Ulid.NewUlid         |  56.24 ns | 0.140 ns | 0.131 ns |  0.09 | 0.0024 |      40 B |          NA | PseudoRandom       |
| Guid.NewGuid         | 595.64 ns | 0.416 ns | 0.368 ns |  1.00 |      - |         - |          NA |                    |
|                      |           |          |          |       |        |           |             |                    |
| Ulid.ParseUtf8String |  77.28 ns | 0.256 ns | 0.239 ns |  2.56 | 0.0024 |      40 B |          NA |                    |
| Ulid.ParseString     |  80.95 ns | 0.300 ns | 0.266 ns |  2.68 | 0.0024 |      40 B |          NA |                    |
| Guid.Parse           |  30.16 ns | 0.064 ns | 0.060 ns |  1.00 |      - |         - |          NA |                    |
|                      |           |          |          |       |        |           |             |                    |
| Ulid.ToString        |  47.93 ns | 0.249 ns | 0.221 ns |  2.93 | 0.0048 |      80 B |        0.83 |                    |
| Guid.ToString        |  16.38 ns | 0.087 ns | 0.082 ns |  1.00 | 0.0057 |      96 B |        1.00 |                    |

Legend:

- Mean      : Arithmetic mean of all measurements
- Error     : Half of 99.9% confidence interval
- StdDev    : Standard deviation of all measurements
- Ratio     : Mean of the ratio distribution ([Current]/[Baseline])
- RatioSD   : Standard deviation of the ratio distribution ([Current]/[Baseline])
- Gen0      : GC Generation 0 collects per 1000 operations
- Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
- 1 ns      : 1 Nanosecond (0.000000001 sec)

[^1]: It looks like the `Guid.NewGuid` is ~10 times "slower" than `Ulid.NewUlid`. But that is because it uses a cryptographic
random number generator on every call, whereas `Ulid.NewUlid` only uses it when the millisecond timestamp changes and if it
doesn't, it simply increments the random part of the previous call.

## Related Packages

- **[vm2.UlidTool](https://github.com/vmelamed/vm2.Ulid/blob/main/src/UlidTool)** - ULID Generator Command Line Tool
- **[ULID Specification](https://github.com/ulid/spec)** - Official ULID spec

## License

MIT - See [LICENSE](https://github.com/vmelamed/vm2.Ulid/blob/main/LICENSE)
