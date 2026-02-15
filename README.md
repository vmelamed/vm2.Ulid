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
- [Install the Package (NuGet)](#install-the-package-nuget)
- [Quick Start](#quick-start)
- [Get the Code](#get-the-code)
- [Build from the Source Code](#build-from-the-source-code)
- [Tests](#tests)
- [Benchmark Tests](#benchmark-tests)
- [Build and Run the Example](#build-and-run-the-example)
- [Basic Usage](#basic-usage)
- [Why Do I Need `UlidFactory`?](#why-do-i-need-ulidfactory)
  - [The `vm2.UlidFactory` Class](#the-vm2ulidfactory-class)
    - [Randomness Provider (`vm2.IRandomNumberGenerator`)](#randomness-provider-vm2irandomnumbergenerator)
    - [Timestamp Provider (`vm2.ITimestampProvider`)](#timestamp-provider-vm2itimestampprovider)
  - [The `UlidFactory` in a Distributed System](#the-ulidfactory-in-a-distributed-system)
- [Performance](#performance)
- [Related Packages](#related-packages)
- [License](#license)

<!-- /TOC -->
## Overview

A small, fast, and spec-compliant .NET package that implements
[Universally Unique Lexicographically Sortable Identifier (ULID)](https://github.com/ulid/spec).

ULIDs combine a 48-bit timestamp (milliseconds since Unix epoch) with 80 bits of randomness, producing compact 128-bit
identifiers that are lexicographically sortable by creation time.

This package exposes a `vm2.Ulid` value type and a `vm2.UlidFactory` for stable, monotonic generation.

## Short Comparison of ULID vs UUID (`System.Guid`)

Universally unique lexicographically sortable identifiers (ULIDs) offer advantages over traditional globally unique
identifiers (GUIDs, or UUIDs) in some scenarios:

- **Lexicographic sorting**: lexicographically sortable identifiers, useful for **database indexing**
- **Timestamp component**: most significant six bytes encode time, enabling **chronological ordering**
- **Monotonic change**: reduced fragmentation for high-frequency generation within the same millisecond
- **Compact representation**: 26-character Crockford Base32 string vs 36-character GUID hex with hyphens (8-4-4-4-12)
- **Readable time hint**: first 10 characters encode the timestamp; GUIDs do not expose creation time in a consistent way
- **Binary compatibility**: 128-bit values, easy integration with GUID-based systems

## Prerequisites

- .NET 10.0 or later

## Install the Package (NuGet)

- Using the dotnet CLI:

  ```bash
  dotnet add package vm2.Ulid
  ```

- From Visual Studio **Package Manager Console**:

  ```powershell
  Install-Package vm2.Ulid
  ```

## Quick Start

- Install package

  ```bash
  dotnet add package vm2.Ulid
  ```

- Generate ULID

  ```csharp
  using vm2;

  UlidFactory factory = new UlidFactory();
  Ulid ulid = factory.NewUlid();
  ```

For testing, database seeding, and other automation, use the [vm2.UlidTool](https://github.com/vmelamed/vm2.Ulid/blob/main/src/UlidTool) CLI.

## Get the Code

You can clone the [GitHub repository](https://github.com/vm2/vm2.Ulid). The project is in the `src/UlidType` directory.

## Build from the Source Code

- Command line:

  ```bash
  dotnet build src/UlidType/UlidType.csproj
  ```

- Visual Studio:
  - Open the solution and choose **Build Solution** (or **Rebuild** as needed).

## Tests

The test project is in the `test` directory. It uses MTP v2 with xUnit v3.2.2. Compatibility varies by Visual Studio version.
Tests are buildable and runnable from the command line using the `dotnet` CLI and from Visual Studio Code across OSes.

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

  - Run the tests standalone:

    ```bash
    test/UlidType.Tests/bin/Debug/net10.0/UlidType.Tests
    ```

## Benchmark Tests

The benchmark tests project is in the `benchmarks` directory. It uses BenchmarkDotNet v0.13.8. Benchmarks are buildable and
runnable from the command line using the `dotnet` CLI.

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

The example is a file-based application `GenerateUlids.cs` in the `examples` directory. It demonstrates basic usage of the
`vm2.Ulid` library. The example is buildable and runnable from the command line using the `dotnet` CLI.

- Command line:

  ```bash
  dotnet run --file examples/GenerateUlids.cs
  ```

  or just:

  ```bash
  dotnet examples/GenerateUlids.cs
  ```

- On a Linux/macOS system with the .NET SDK installed, you can also run the example app directly:

  ```bash
  examples/GenerateUlids.cs
  ```

  Provided that:
  - execute permission set
  - first line ends with `\n` (LF), not `\r\n` (CRLF)
  - no UTF-8 Byte Order Mark (BOM) at the beginning

  These conditions can be met by running the following commands on a Linux system:

  ```bash
  chmod u+x examples/GenerateUlids.cs
  dos2unix examples/GenerateUlids.cs
  ```

## Basic Usage

```csharp
using vm2;

// Recommended: reuse multiple UlidFactory instances, e.g. one per table or entity type.
// Ensures independent monotonicity per context.

UlidFactory factory = new UlidFactory();

Ulid ulid1 = factory.NewUlid();
Ulid ulid2 = factory.NewUlid();

// Default internal factory ensures thread safety and same-millisecond monotonicity across contexts.

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

## Why Do I Need `UlidFactory`?

ULIDs must increase monotonically within the same millisecond. When multiple ULIDs are generated in a single millisecond, each
subsequent ULID is greater by one in the least significant byte(s). A ULID factory tracks the timestamp and the last random
bytes for each call. When the timestamp matches the previous generation, the factory increments the prior random part instead of
generating a new random value.

### The `vm2.UlidFactory` Class

The `vm2.UlidFactory` class encapsulates the requirements and exposes a simple interface for generating ULIDs. Use multiple
`vm2.UlidFactory` instances when needed, e.g. one per database table or entity type.

In simple scenarios, use the static method `vm2.Ulid.NewUlid()` instead of `vm2.UlidFactory`. It uses a single internal static
factory instance with a cryptographic random number generator.

ULID factories are thread-safe and ensure monotonicity of generated ULIDs across application contexts.
The factory uses two providers: one for the random bytes and one for the timestamp.

Use dependency injection to construct the factory and manage the providers. DI keeps the provider lifetimes explicit, makes
testing simple, and enforces a single, consistent configuration across the app or service.

#### Randomness Provider (`vm2.IRandomNumberGenerator`)

By default the `vm2.UlidFactory` uses a thread-safe, cryptographic random number generator
(`vm2.UlidRandomProviders.CryptoRandom`), which is suitable for most applications. If you need a different source of randomness,
e.g. for testing purposes, for performance reasons, or if you are concerned about your source of entropy (`/dev/random`), you
can explicitly specify that the factory should use the pseudo-random number generator `vm2.UlidRandomProviders.PseudoRandom`.
You can also provide your own, thread-safe implementation of `vm2.IRandomNumberGenerator` to the factory.

#### Timestamp Provider (`vm2.ITimestampProvider`)

By default, the timestamp provider uses `DateTime.UtcNow` converted to Unix epoch time in milliseconds. If you need a different
source of time, e.g. for testing purposes, you can provide your own implementation of `vm2.ITimestampProvider` to the factory.

### The `UlidFactory` in a Distributed System

In distributed database applications and services, ULIDs are often generated across many nodes. Design for collision avoidance
and monotonicity from the start. Node-local monotonicity does not imply global monotonicity, and clock skew can surface quickly
under load.

One approach uses a separate `UlidFactory` instance on each node with a unique node identifier. ULIDs remain distinct even when
generated in the same millisecond. However, global monotonicity across all nodes does not hold under this approach.

To maintain global monotonicity, a centralized ULID service can generate ULIDs for all nodes. This ensures uniqueness and
monotonicity across the system, at the cost of a single point of failure and a potential performance bottleneck. Time
synchronization across nodes remains a challenge; clock skew can cause non-monotonic ULIDs if not handled properly.

Another approach uses a consensus algorithm to coordinate ULID generation across nodes. This adds complexity and overhead.

The choice depends on system requirements and constraints. Consider trade-offs among uniqueness, monotonicity, performance, and
complexity when designing a distributed ULID strategy.

## Performance

Benchmark results vs similar Guid-generating functions, run on GitHub Actions:

```text
BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)

AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores. .NET SDK 9.0.305

- [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
- DefaultJob : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
```

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

- **[ULID Specification](https://github.com/ulid/spec)** - Official ULID spec
- **[vm2.UlidTool](https://github.com/vmelamed/vm2.Ulid/blob/main/src/UlidTool)** - ULID Generator Command Line Tool

## License

MIT - See [LICENSE](https://github.com/vmelamed/vm2.Ulid/blob/main/LICENSE)
