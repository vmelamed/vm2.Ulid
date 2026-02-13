# Universally Unique Lexicographically Sortable Identifier (ULID) for .NET

[![CI](https://github.com/vmelamed/vm2.Ulid/actions/workflows/CI.yaml/badge.svg?branch=main)](https://github.com/vmelamed/vm2.Ulid/actions/workflows/CI.yaml)
[![codecov](https://codecov.io/gh/vmelamed/vm2.Ulid/branch/main/graph/badge.svg?branch=main)](https://codecov.io/gh/vmelamed/vm2.Ulid)
[![Release](https://github.com/vmelamed/vm2.Ulid/actions/workflows/Release.yaml/badge.svg?branch=main)](https://github.com/vmelamed/vm2.Ulid/actions/workflows/Release.yaml)
[![NuGet](https://img.shields.io/nuget/v/vm2.Ulid.Api.svg?branch=main)](https://www.nuget.org/packages/vm2.Ulid.Api)
[![NuGet Downloads](https://img.shields.io/nuget/dt/vm2.Ulid.Api.svg?branch=main)](https://www.nuget.org/packages/vm2.Ulid.Api/)
[![License](https://img.shields.io/github/license/vmelamed/vm2.Ulid)](https://github.com/vmelamed/vm2.Ulid/blob/main/LICENSE)

<!-- TOC tocDepth:2..5 chapterDepth:2..6 -->

- [Overview](#overview)
- [Short Comparison of ULID vs UUID (`System.Guid`)](#short-comparison-of-ulid-vs-uuid-systemguid)
- [Prerequisites](#prerequisites)
- [Basic Usage](#basic-usage)
- [Why do I need `UlidFactory`?](#why-do-i-need-ulidfactory)
- [Get the code](#get-the-code)
- [Install the package (NuGet)](#install-the-package-nuget)
- [Performance](#performance)

<!-- /TOC -->
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

## Prerequisites

- .NET 9.0 or later

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

Ulid.TryParse(ulid1String, out var ulidCopy1);           // parse ULID from UTF-16 string (26 UTF-16 characters)
Ulid.TryParse(ulidUtf8String, out var ulidCopy2);       // parse ULID from its UTF-8 string (26 UTF-8 characters/bytes)

Debug.Assert(ulid1 == ulidCopy1 &&                      // Parsed ULIDs are equal to the original
             ulid1 == ulidCopy2);
```

## Why do I need `UlidFactory`?

One of the requirements for the ULIDs is that they increase monotonically within the same millisecond. This means that if you
generate multiple ULIDs in the same millisecond, each subsequent ULID must be greater than the previous one by one in the least
significant byte(s). To ensure that, there must be a ULID generating object (factory) that keeps track of the timestamp and the
last generated random bytes. If during generation of a ULID the factory finds out that the current timestamp is the same as the
timestamp of the previous generation to the millisecond, the factory must increment the previous random part and use it, rather
than generating new randomness.

The `UlidFactory` class encapsulates this logic, providing a simple interface for generating ULIDs that meet this requirement.

It is prudent to create and reuse more than one `UlidFactory` instances, e.g. one per DB table or entity type which require
ULIDs. The ULID factory(s) are thread-safe and ensure monotonicity of the generated ULIDs in different contexts of an
application or a service.

By default the `UlidFactory` uses a cryptographic random number generator (`vm2.UlidRandomProviders.CryptoRandom`), which is
suitable for most applications. If you need a different source of randomness (e.g. for testing or performance reasons) or you
are concerned about the entropy source, you can explicitly specify that the factory should use the pseudo-random number
generator `vm2.UlidRandomProviders.PseudoRandom`. You can also provide your own, thread-safe implementation of
`vm2.IRandomNumberGenerator` to the factory.

In contrast, if you do not want to use the `vm2.UlidFactory` directly, you can use the static `vm2.Ulid.NewUlid()` method, which
uses an internal static factory instance with a cryptographic random number generator.

## Get the code

Clone this repository:

  ```bash
  git clone https://github.com/vmelamed/vm2.Ulid.git
  cd vm2.Ulid
  ```

## Install the package (NuGet)

If a NuGet package is published for this project you can add it

- with the dotnet CLI:

  ```bash
  dotnet add vm2.Ulid package vm2.Ulid
  ```

- Or from Visual Studio use the **Package Manager Console**:

  ```powershell
  Install-Package vm2.Ulid
  ```

Build from source

- Command line:

  ```bash
  dotnet build src/UlidType/UlidType.csproj
  ```

- Visual Studio:
-
  - Open the solution and choose **Build Solution** (or use **Rebuild** as needed).

Run the example:

  ```bash
  dotnet run --project examples/GenerateUlids/GenerateUlids.csproj
  ```

## Performance

You can build and run the benchmark tests in release mode with:

```sh
benchmarks/UlidType.Benchmarks/bin/Release/net9.0/UlidType.Benchmarks.exe --filter * --memory --artifacts ../../../BenchmarkDotNet.Artifacts
```

Here are some benchmark results with similar Guid functions as baselines run on GitHub Actions:

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)

AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores .NET SDK 9.0.305

- [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
- DefaultJob : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

| Method               | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio | RandomProviderType |
|--------------------  |----------:|---------:|---------:|------:|-------:|----------:|------------:|------------------- |
| UlidFactory.NewUlid  |  56.12 ns | 0.085 ns | 0.071 ns |  0.09 | 0.0024 |      40 B |          NA | CryptoRandom       |
| Ulid.NewUlid         |  56.49 ns | 0.105 ns | 0.098 ns |  0.09 | 0.0024 |      40 B |          NA | CryptoRandom       |
| Guid.NewGuid[^1]     | 595.28 ns | 1.149 ns | 0.897 ns |  1.00 |      - |         - |          NA | CryptoRandom       |
|                      |           |          |          |       |        |           |             |                    |
| UlidFactory.NewUlid  |  55.99 ns | 0.106 ns | 0.094 ns |  0.09 | 0.0024 |      40 B |          NA | PseudoRandom       |
| Ulid.NewUlid         |  56.24 ns | 0.140 ns | 0.131 ns |  0.09 | 0.0024 |      40 B |          NA | PseudoRandom       |
| Guid.NewGuid         | 595.64 ns | 0.416 ns | 0.368 ns |  1.00 |      - |         - |          NA | PseudoRandom       |
|                      |           |          |          |       |        |           |             |                    |
| Guid.Parse           |  30.16 ns | 0.064 ns | 0.060 ns |  1.00 |      - |         - |          NA |                    |
| Ulid.ParseUtf8String |  77.28 ns | 0.256 ns | 0.239 ns |  2.56 | 0.0024 |      40 B |          NA |                    |
| Ulid.ParseString     |  80.95 ns | 0.300 ns | 0.266 ns |  2.68 | 0.0024 |      40 B |          NA |                    |
|                      |           |          |          |       |        |           |             |                    |
| Guid.ToString        |  16.38 ns | 0.087 ns | 0.082 ns |  1.00 | 0.0057 |      96 B |        1.00 |                    |
| Ulid.ToString        |  47.93 ns | 0.249 ns | 0.221 ns |  2.93 | 0.0048 |      80 B |        0.83 |                    |

Legend:

- Mean      : Arithmetic mean of all measurements
- Error     : Half of 99.9% confidence interval
- StdDev    : Standard deviation of all measurements
- Ratio     : Mean of the ratio distribution ([Current]/[Baseline])
- RatioSD   : Standard deviation of the ratio distribution ([Current]/[Baseline])
- Gen0      : GC Generation 0 collects per 1000 operations
- Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
- 1 ns      : 1 Nanosecond (0.000000001 sec)

[^1]: `Guid.NewGuid` is ~10 times slower than `Ulid.NewUlid` because it uses a cryptographic random number generator on every
  call, whereas `Ulid.NewUlid` only uses it when the millisecond timestamp changes and if it doesn't, it just increments the
  previous random part.
