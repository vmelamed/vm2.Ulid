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
  - [Short Comparison of ULID vs Random UUID (`Guid.NewGuid`)](#short-comparison-of-ulid-vs-random-uuid-guidnewguid)
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
      - [Randomness Provider (`vm2.IUlidRandomProvider`)](#randomness-provider-vm2iulidrandomprovider)
      - [Timestamp Provider (`System.TimeProvider`)](#timestamp-provider-systemtimeprovider)
      - [Serialization](#serialization)
    - [The `UlidFactory` in a Distributed System](#the-ulidfactory-in-a-distributed-system)
  - [Performance](#performance)
  - [Related Documents and Packages](#related-documents-and-packages)
  - [License](#license)

<!-- /TOC -->
## Overview

A small, fast, AOT-ready, and **spec-compliant** .NET package that implements
[Universally Unique Lexicographically Sortable Identifier (ULID)](https://github.com/ulid/spec).

ULIDs combine a 48-bit timestamp (milliseconds since Unix epoch) with 80 bits of randomness, producing compact 128-bit
identifiers that are lexicographically sortable by creation time.

This package exposes a `vm2.Ulid` value type and a `vm2.UlidFactory` for stable, monotonic generation.

## Short Comparison of ULID vs Random UUID (`Guid.NewGuid`)

Universally unique lexicographically sortable identifiers (ULIDs) offer advantages over random (version 4) universally
unique identifiers (UUIDs, a.k.a. GUIDs) - the kind `Guid.NewGuid()` creates - in some scenarios:

- **Sortable**: ULIDs sort by creation time, both as 128-bit values and as strings, which keeps **database index**
  inserts local instead of scattering them across the index the way random UUIDs do
- **Timestamp component**: the six most significant bytes encode the time, enabling **chronological ordering**
- **Monotonic generation**: ULIDs created by the same factory within the same millisecond are strictly increasing
- **Compact representation**: a 26-character Crockford Base32 string vs. a 36-character hexadecimal GUID with hyphens
  (8-4-4-4-12)
- **Readable time hint**: the first 10 characters encode the timestamp; random UUIDs carry no time at all
- **Binary compatibility**: a ULID is a 128-bit value, the same size as a GUID, and converts to and from `System.Guid`.
  **However**, the converted GUID does not keep the ULID sort order when stored in a SQL Server `uniqueidentifier` or a
  PostgreSQL `uuid` column: both order GUIDs by byte groups that do not start with the ULID timestamp. To keep the
  ordering, store ULIDs as 16-byte binary values (`binary(16)`, `bytea`) or as strings.

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

You can clone the [GitHub repository](https://github.com/vmelamed/vm2.Ulid). The project is in the `src/Ulid` directory.

## Build from the Source Code

- Command line:

  ```bash
  dotnet build src/Ulid/Ulid.csproj
  ```

- Visual Studio:
  - Open the solution `vm2.Ulid.slnx` and choose **Build Solution** (or **Rebuild** as needed).

The build writes its output under the `artifacts` directory (`artifacts/bin/<project>/<configuration>/`), not under each
project's `bin` directory.

## Tests

The test projects are in the `tests` directory. They use xUnit v3 on Microsoft Testing Platform (MTP) v2; compatibility
varies by Visual Studio version. You can build and run the tests from the command line with the `dotnet` CLI, and from
Visual Studio Code on any OS.

- Command line:

  ```bash
  dotnet test --project tests/Ulid/Ulid.Tests.csproj
  ```

- You can also run the tests standalone after building the solution or the test project:

  - Build the solution or only the test project:

    ```bash
    dotnet build                                # build the full solution, or
    dotnet build tests/Ulid/Ulid.Tests.csproj   # only the test project
    ```

  - Run the tests standalone:

    ```bash
    artifacts/bin/Ulid.Tests/debug/Ulid.Tests
    ```

## Benchmark Tests

The benchmark project is in the `benchmarks` directory. It uses BenchmarkDotNet, and you can build and run it from the
command line with the `dotnet` CLI in the **Release** configuration.

- Command line:

  ```bash
  dotnet run --project benchmarks/Ulid/Ulid.Benchmarks.csproj -c Release
  ```

- You can also run the benchmarks standalone after building the benchmark project:

  - Build only the benchmark project:

    ```bash
    dotnet build -c Release benchmarks/Ulid/Ulid.Benchmarks.csproj
    ```

  - Run the benchmarks standalone (Linux/macOS):

    ```bash
    artifacts/bin/Ulid.Benchmarks/release/Ulid.Benchmarks --filter '*' --join --exporters json markdown --memory
    ```

  - Run the benchmarks standalone (Windows):

    ```bash
    artifacts/bin/Ulid.Benchmarks/release/Ulid.Benchmarks.exe --filter '*' --join --exporters json markdown --memory
    ```

- You can also run the benchmarks with the Bash script `run-benchmarks.sh` from the `vm2.DevOps` repository, e.g.:

  ```bash
  bash "$VM2_REPOS/vm2.DevOps/.github/scripts/run-benchmarks.sh" "$VM2_REPOS/vm2.Ulid/benchmarks/Ulid/Ulid.Benchmarks.csproj"
  ```

## Build and Run the Example

The example is a file-based application `GenerateUlids.cs` in the `examples` directory. It demonstrates basic usage of the
`vm2.Ulid` library. You can build and run it from the command line with the `dotnet` CLI.

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
  - the file has the execute permission
  - the first line ends with `\n` (LF), not `\r\n` (CRLF)
  - the file does not start with a UTF-8 Byte Order Mark (BOM)

  You can meet these conditions by running the following commands on a Linux system (`dos2unix` also removes the BOM):

  ```bash
  chmod u+x examples/GenerateUlids.cs
  dos2unix examples/GenerateUlids.cs
  ```

## Basic Usage

```csharp
using System.Diagnostics;
using System.Text;
using vm2;

// Each UlidFactory instance is an independent, monotonic sequence of ULIDs.
// You may use several instances, e.g. one per table or entity type.
UlidFactory factory = new UlidFactory();

Ulid ulid1 = factory.NewUlid();
Ulid ulid2 = factory.NewUlid();

// The static Ulid.NewUlid() uses an internal, shared factory: thread-safe and monotonic for all of its callers.
Ulid ulid = Ulid.NewUlid();

Debug.Assert(ulid1 != ulid2);                                       // uniqueness
Debug.Assert(ulid1 < ulid2);                                        // monotonic within the same factory

var ulid1String = ulid1.ToString();                                 // the canonical string representation
var ulid2String = ulid2.ToString();

Debug.Assert(ulid1String != ulid2String);                           // ULID strings are unique
Debug.Assert(string.CompareOrdinal(ulid1String, ulid2String) < 0);  // and sort in the same order as the ULIDs
Debug.Assert(ulid1String.Length == 26);                             // the string is 26 characters long

Debug.Assert(ulid1.Timestamp <= ulid2.Timestamp);                   // the timestamp can be extracted
Debug.Assert(ulid1.RandomBytes.Length == 10);                       // a ULID has 10 bytes (80 bits) of randomness
Debug.Assert(ulid1.Bytes.Length == 16);                             // a ULID is a 16-byte (128-bit) value

var ulidGuid     = ulid1.ToGuid();                                  // a ULID can be converted to a Guid
var ulidFromGuid = new Ulid(ulidGuid);                              // and created from a Guid

Debug.Assert(ulidFromGuid == ulid1);

var ulidUtf8String = Encoding.UTF8.GetBytes(ulid1String);

Ulid.TryParse(ulid1String, out var ulidCopy1);                      // parse from a UTF-16 string (26 characters)
Ulid.TryParse(ulidUtf8String, out var ulidCopy2);                   // parse from a UTF-8 string (26 bytes)

Debug.Assert(ulid1 == ulidCopy1 &&                                  // the parsed ULIDs are equal to the original
             ulid1 == ulidCopy2);
```

## Why Do I Need `UlidFactory`?

The ULID specification defines an optional *monotonic* mode for ULIDs generated within the same millisecond: each subsequent
ULID is the previous one plus one, i.e. the 80-bit random part is incremented instead of drawn anew. This keeps ULIDs from one
generator in generation order even within a millisecond. A ULID factory implements this mode: it remembers the timestamp and
the random part of the last ULID it generated. When the timestamp matches the previous one, the factory increments the previous
random part instead of generating a new random value.

### The `vm2.UlidFactory` Class

The `vm2.UlidFactory` class encapsulates the requirements and exposes a simple interface for generating ULIDs. Use multiple
`vm2.UlidFactory` instances when needed, e.g. one per aggregate root or database table.

Each factory instance is thread-safe and generates a strictly increasing sequence of ULIDs within its process. Separate
instances are independent sequences: ULIDs from different factories are ordered only by their timestamps. The factory uses two
providers: one for the random bytes and one for the timestamp.

Use dependency injection to construct the factory and manage the providers. DI keeps the provider lifetimes explicit, makes
testing simple, and enforces a single, consistent configuration across the app or service.

In simple scenarios, use the static method `vm2.Ulid.NewUlid()` instead of `vm2.UlidFactory`. It uses an internal single static
factory instance with a cryptographic random number generator and a clock based on `System.TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()`.

#### Randomness Provider (`vm2.IUlidRandomProvider`)

By default, `vm2.UlidFactory` uses a thread-safe, cryptographically secure random number generator
(`vm2.Providers.Ulid.CryptoRandom`), as the ULID specification recommends. It suits most applications. For tests, or where
predictable random parts are acceptable, you can explicitly specify the pseudo-random number generator
`vm2.Providers.Ulid.PseudoRandom` instead. You can also give the factory your own thread-safe implementation of
`vm2.IUlidRandomProvider`.

#### Timestamp Provider (`System.TimeProvider`)

By default, the timestamp provider uses `System.TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()`. For a different
time source, use a class derived from `System.TimeProvider`, e.g. for testing use
`Microsoft.Extensions.Time.Testing.FakeTimeProvider` (package `Microsoft.Extensions.TimeProvider.Testing`).

#### Serialization

The `vm2.Ulid` type is marked with `[JsonConverter(typeof(UlidConverter))]`, so `System.Text.Json` serializes and deserializes
it without any additional configuration. For Newtonsoft.Json, use the companion package `vm2.Ulid.Serialization.NsJson`
([source code](https://github.com/vmelamed/vm2.Ulid/blob/main/src/Serialization.NsJson.Ulid)).

> [!WARNING]
> The `vm2.Ulid.Serialization.NsJson` package depends on Newtonsoft.Json, which is not AOT-compatible.

### The `UlidFactory` in a Distributed System

In a distributed application, ULIDs are usually generated on many nodes at once. Decide early which ordering guarantee you
actually need, because each stronger guarantee costs more.

**Uniqueness needs no coordination.** Each node can use its own `UlidFactory`; no node identifier is needed. The 80 random
bits make collisions negligible: even a million ULIDs generated across all nodes within the same millisecond collide with a
probability below 10^-12.

**Ordering across nodes is approximate.** ULIDs from one factory are strictly increasing. ULIDs from different factories or
nodes are ordered only by their timestamps: within the same millisecond their order is random, and clock skew between nodes
can make a ULID generated later on one node sort before one generated earlier on another. For database index locality, this
is almost always good enough.

**A central ULID service gives one total order of issuance.** All nodes request their ULIDs from a single generator per
sequence, so only the service's clock matters. The costs are a network round-trip per ULID (or per batch) and a dependency
that must stay available. Replicas with leader election remove the single point of failure, but a new leader must never issue
ULIDs smaller than the ones its predecessor issued, e.g. it starts after a persisted timestamp high-water mark.

> [!IMPORTANT]
> Monotonic *issuance* is not monotonic *visibility*. Even with a central service, a ULID issued earlier can be committed
> later than one issued after it. A consumer that reads "everything after the last ULID I saw" (cursor pagination, change
> feeds, outboxes) can therefore miss rows. Such consumers need a watermark, e.g. they read only ULIDs older than the longest
> expected transaction.

Choose based on your requirements, weighing uniqueness, ordering, latency, availability, and complexity.

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

`Guid.NewGuid` asks the operating system's cryptographic random number generator for fresh random bytes on every call (a
system call). `Ulid.NewUlid` does that only when the millisecond timestamp changes; within the same millisecond it simply
increments the random part of the previous ULID. The benchmark generates ULIDs in a tight loop, so most calls take this cheap
path. When the calls are more than a millisecond apart, each one pays for the random number generator.

## Related Documents and Packages

- **[ULID Specification](https://github.com/ulid/spec)** - Official ULID spec
- **[vm2.Ulid.Serialization.NsJson](https://github.com/vmelamed/vm2.Ulid/blob/main/src/Serialization.NsJson.Ulid)** - Companion package: Newtonsoft.Json converter for vm2.Ulid
- **[vm2.UlidTool](https://github.com/vmelamed/vm2.Ulid/blob/main/src/UlidTool)** - ULID Generator Command Line Tool
- **[Example](https://github.com/vmelamed/vm2.Ulid/blob/main/examples/GenerateUlids.cs)** - Basic usage example

## License

MIT - See [LICENSE](https://github.com/vmelamed/vm2.Ulid/blob/main/LICENSE)
