# Universally Unique Lexicographically Sortable Identifier (ULID) for .NET

[![CI](https://github.com/vmelamed/vm2.Ulid/actions/workflows/CI.yaml/badge.svg?branch=main)](https://github.com/vmelamed/vm2.Ulid/actions/workflows/CI.yaml)
[![codecov](https://codecov.io/gh/vmelamed/vm2.Ulid/branch/main/graph/badge.svg?branch=main)](https://codecov.io/gh/vmelamed/vm2.Ulid)
[![Release](https://github.com/vmelamed/vm2.Ulid/actions/workflows/Release.yaml/badge.svg?branch=main)](https://github.com/vmelamed/vm2.Ulid/actions/workflows/Release.yaml)

[![NuGet Version](https://img.shields.io/nuget/v/vm2.Ulid)](https://www.nuget.org/packages/vm2.Ulid/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/vm2.Ulid.svg)](https://www.nuget.org/packages/vm2.Ulid/)
[![GitHub License](https://img.shields.io/github/license/vmelamed/vm2.Ulid)](https://github.com/vmelamed/vm2.Ulid/blob/main/LICENSE)

<!-- TOC tocDepth:2..6 chapterDepth:2..6 -->

- [Universally Unique Lexicographically Sortable Identifier (ULID) for .NET](#universally-unique-lexicographically-sortable-identifier-ulid-for-net)
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
      - [Timestamp Provider (`System.TimeProvider`)](#timestamp-provider-systemtimeprovider)
      - [Serialization](#serialization)
    - [The `UlidFactory` in a Distributed System](#the-ulidfactory-in-a-distributed-system)
  - [Performance](#performance)
  - [Related Documents and Packages](#related-documents-and-packages)
  - [License](#license)

<!-- /TOC -->
## Overview

A small, fast, AoT ready, and **spec-compliant** .NET package that implements
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

You can clone the [GitHub repository](https://github.com/vm2/vm2.Ulid). The project is in the `src/Ulid` directory.

## Build from the Source Code

- Command line:

  ```bash
  dotnet build src/Ulid/Ulid.csproj
  ```

- Visual Studio:
  - Open the solution and choose **Build Solution** (or **Rebuild** as needed).

## Tests

The test project is in the `test` directory. It uses MTP v2 with xUnit v3.2.2. Compatibility varies by Visual Studio version.
Tests are buildable and runnable from the command line using the `dotnet` CLI and from Visual Studio Code across OSes.

- Command line:

  ```bash
  dotnet test --project tests/Ulid.Tests/Ulid.Tests.csproj
  ```

- The tests can also be run standalone after building the solution or the test project:

  - build the solution or the test project only:

    ```bash
    dotnet build # build the full solution or
    dotnet build tests/Ulid.Tests/Ulid.Tests.csproj # the test project only
    ```

  - Run the tests standalone:

    ```bash
    tests/Ulid.Tests/bin/Debug/net10.0/Ulid.Tests
    ```

## Benchmark Tests

The benchmark tests project is in the `benchmarks` directory. It uses BenchmarkDotNet Benchmarks are buildable and runnable from the command line using the `dotnet` in **Release** configuration.

- Command line:

  ```bash
  dotnet run --project benchmarks/Ulid.Benchmarks/Ulid.Benchmarks.csproj -c Release
  ```

- The benchmarks can also be run standalone after building the benchmark project:

  - build the benchmark project only:

    ```bash
    dotnet build -c Release benchmarks/Ulid.Benchmarks/Ulid.Benchmarks.csproj
    ```

  - Run the benchmarks standalone (Linux/macOS):

    ```bash
    benchmarks/Ulid.Benchmarks/bin/Release/net10.0/Ulid.Benchmarks --filter '*' --join --exporters json markdown --memory
    ```

  - Run the benchmarks standalone (Windows):

    ```bash
    benchmarks/Ulid.Benchmarks/bin/Release/net10.0/Ulid.Benchmarks.exe --filter '*' --join --exporters json markdown --memory
    ```

- You can also use the Bash script `run-benchmarks.sh` from the `vm2.DevOps` repository to run the benchmarks, e.g.:

  ```bash
  bash vm2.DevOps/.github/scripts/run-benchmarks.sh $VM2_REPOS/vm2.Ulid/benchmarks/Ulid/Ulid.Benchmarks.csproj
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
`vm2.UlidFactory` instances when needed, e.g. one per aggregate root or database table.

ULID factories are thread-safe and ensure monotonicity of generated ULIDs across application contexts. The factory uses two providers: one for the random bytes and one for the timestamp.

Use dependency injection to construct the factory and manage the providers. DI keeps the provider lifetimes explicit, makes
testing simple, and enforces a single, consistent configuration across the app or service.

In simple scenarios, use the static method `vm2.Ulid.NewUlid()` instead of `vm2.UlidFactory`. It uses an internal single static
factory instance with a cryptographic random number generator and a clock based on `System.TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()`.

#### Randomness Provider (`vm2.IRandomNumberGenerator`)

By default the `vm2.UlidFactory` uses a thread-safe, cryptographic random number generator
(`vm2.UlidRandomProviders.CryptoRandom`), which is suitable for most applications. If you need a different source of randomness,
e.g. for testing purposes, for performance reasons, or if you are concerned about your source of entropy (`/dev/random`), you
can explicitly specify that the factory should use the pseudo-random number generator `vm2.UlidRandomProviders.PseudoRandom`.
You can also provide your own, thread-safe implementation of `vm2.IRandomNumberGenerator` to the factory.

#### Timestamp Provider (`System.TimeProvider`)

By default, the timestamp provider uses `System.TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()`. For a different time source use a class derived from `System.TimeProvider`, e.g. for testing use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` (package `Microsoft.Extensions.TimeProvider.Testing`).

#### Serialization

The `vm2.Ulid` type is marked with the `System.Text.Json.Serialization.JsonConverterAttribute` attribute, so it can be serialized and deserialized by `System.Text.Json` without any additional configuration. For Newtonsoft.Json, use the companion package `vm2.UlidSerialization.NsJson` ([source code](https://github.com/vmelamed/vm2.Ulid/blob/main/src/UlidNsConverter)).

> [!WARNING]
> The `vm2.UlidSerialization.NsJson` package depends on Newtonsoft.Json, which is not AoT compatible.

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

```text
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
```

| Type         | Method                  | RandomProviderType | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |------------------------ |------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| **ToString** | **Guid.ToString**       | **?**              |  **14.43 ns** | **0.065 ns** | **0.058 ns** |  **1.00** |    **0.01** | **0.0057** |      **96 B** |        **1.00** |
| **Parse**| **Guid.Parse(string)**  | **?**              |  **28.33 ns** | **0.095 ns** | **0.089 ns** |  **1.96** |    **0.01** |      - |         - |        **0.00** |
| ToString | Ulid.ToString           | ?                  |  43.27 ns | 0.189 ns | 0.177 ns |  3.00 |    0.02 | 0.0048 |      80 B |        0.83 |
| Parse    | Ulid.Parse(StringUtf16) | ?                  |  64.57 ns | 0.676 ns | 0.632 ns |  4.48 |    0.05 | 0.0023 |      40 B |        0.42 |
| Parse    | Ulid.Parse(StringUtf8)  | ?                  |  55.10 ns | 0.168 ns | 0.149 ns |  3.82 |    0.02 | 0.0024 |      40 B |        0.42 |
|              |                         |                    |           |          |          |       |         |        |           |             |
| **New**      | **Guid.NewGuid**            | **N/A**       | **612.36 ns** | **1.952 ns** | **1.826 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| New      | Ulid.NewUlid            | CryptoRandom       |  62.14 ns | 0.277 ns | 0.259 ns |  0.10 |    0.00 | 0.0023 |      40 B |          NA |
| New      | Factory.NewUlid         | CryptoRandom       |  62.06 ns | 0.392 ns | 0.366 ns |  0.10 |    0.00 | 0.0023 |      40 B |          NA |
| New      | Ulid.NewUlid            | PseudoRandom       |  62.10 ns | 0.430 ns | 0.402 ns |  0.10 |    0.00 | 0.0023 |      40 B |          NA |
| New      | Factory.NewUlid         | PseudoRandom       |  61.92 ns | 0.329 ns | 0.308 ns |  0.10 |    0.00 | 0.0023 |      40 B |          NA |

Legend:

- Mean      : Arithmetic mean of all measurements
- Error     : Half of 99.9% confidence interval
- StdDev    : Standard deviation of all measurements
- Gen0      : GC Generation 0 collects per 1000 operations
- Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
- 1 ns      : 1 Nanosecond (0.000000001 or 10^-9 sec)

Guid.NewGuid employs the full UUID (Universally Unique Identifier) algorithm (including the use of the random number generator) on every call, whereas `Ulid.NewUlid` only does that when the millisecond timestamp changes and if it doesn't, it simply increments the random part of the previous call.

## Related Documents and Packages

- **[ULID Specification](https://github.com/ulid/spec)** - Official ULID spec
- **[vm2.UlidSerialization.NsJson](https://github.com/vmelamed/vm2.Ulid/blob/main/src/UlidNsConverter)** - Companion package: Newtonsoft.Json converter for vm2.Ulid
- **[vm2.UlidTool](https://github.com/vmelamed/vm2.Ulid/blob/main/src/UlidTool)** - ULID Generator Command Line Tool
- **[Example](https://github.com/vmelamed/vm2.Ulid/blob/main/examples/GenerateUlids.cs)** - Basic usage example

## License

MIT - See [LICENSE](https://github.com/vmelamed/vm2.Ulid/blob/main/LICENSE)
