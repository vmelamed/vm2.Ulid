# Universally Unique Lexicographically Sortable Identifier (ULID) for .NET

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

By default the `UlidFactory` uses a cryptographic random number generator (`vm2.UlidRandomProviders.CryptoRandom`), which is
suitable for most applications. If you need a different source of randomness (e.g. for testing or performance reasons) or you
are concerned about the entropy source, you can explicitly specify that the factory should use the pseudo-random number
generator `vm2.UlidRandomProviders.PseudoRandom`. You can also provide your own, thread-safe implementation of
`vm2.IRandomNumberGenerator` to the factory.

In contrast, if you do not want to use the `vm2.UlidFactory` directly, at all, you can use the static `vm2.Ulid.NewUlid()`
method, which uses an internal instance of the factory with a cryptographic random number generator.

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

Run the example:

  ```bash
  dotnet run --project examples/GenerateUlids/GenerateUlids.csproj
  ```

## Performance

You can build and run the benchmark tests in release mode with:

```
benchmarks/UlidType.Benchmarks/bin/Release/net9.0/UlidType.Benchmarks.exe --filter * --memory --artifacts ../../../BenchmarkDotNet.Artifacts
```

Here are some benchmark results with similar Guid functions as baselines from GitHub Actions:

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

| Method               | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | RandomProviderType |
|--------------------  |----------:|---------:|---------:|------:|-------:|----------:|------------------- |
| Ulid.NewUlid         |  65.72 ns | 0.067 ns | 0.052 ns |  0.11 | 0.0024 |      40 B | CryptoRandom       |
| UlidFactory.NewUlid  |  72.59 ns | 0.078 ns | 0.065 ns |  0.12 | 0.0024 |      40 B | CryptoRandom       |
| Guid.NewGuid         | 593.35 ns | 0.988 ns | 0.825 ns |  1.00 |      - |         - | CryptoRandom       |
|                      |           |          |          |       |        |           |                    |
| Ulid.NewUlid         |  66.18 ns | 0.079 ns | 0.066 ns |  0.11 | 0.0024 |      40 B | PseudoRandom       |
| UlidFactory.NewUlid  |  66.27 ns | 0.231 ns | 0.216 ns |  0.11 | 0.0024 |      40 B | PseudoRandom       |
| Guid.NewGuid         | 593.63 ns | 1.013 ns | 0.898 ns |  1.00 |      - |         - | PseudoRandom       |
|                      |           |          |          |       |        |           |
| Guid.Parse           |  30.89 ns | 0.048 ns | 0.043 ns |  1.00 |      - |         - |
| Ulid.ParseUtf8String |  76.92 ns | 0.261 ns | 0.218 ns |  2.49 | 0.0024 |      40 B |
| Ulid.ParseString     |  79.54 ns | 0.141 ns | 0.125 ns |  2.58 | 0.0024 |      40 B |
|                      |           |          |          |       |        |           |
| Guid.ToString        |  16.50 ns | 0.061 ns | 0.054 ns |  1.00 | 0.0057 |      96 B |
| Ulid.ToString        |  47.52 ns | 0.135 ns | 0.126 ns |  2.88 | 0.0048 |      80 B |

| Method              | RandomProviderType | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |------------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| UlidFactory.NewUlid | CryptoRandom       | 47.03 ns | 0.969 ns | 2.636 ns | 46.28 ns |  0.93 |    0.06 | 0.0032 |      40 B |          NA |
| Ulid.NewUlid        | CryptoRandom       | 50.25 ns | 1.028 ns | 1.631 ns | 50.17 ns |  0.99 |    0.05 | 0.0032 |      40 B |          NA |
| Guid.NewGuid        | CryptoRandom       | 50.77 ns | 1.044 ns | 1.909 ns | 50.60 ns |  1.00 |    0.05 |      - |         - |          NA |
|                     |                    |          |          |          |          |       |         |        |           |             |
| Guid.NewGuid        | PseudoRandom       | 44.54 ns | 0.889 ns | 1.058 ns | 44.41 ns |  1.00 |    0.03 |      - |         - |          NA |
| UlidFactory.NewUlid | PseudoRandom       | 47.73 ns | 0.959 ns | 1.376 ns | 47.82 ns |  1.07 |    0.04 | 0.0032 |      40 B |          NA |
| Ulid.NewUlid        | PseudoRandom       | 48.80 ns | 1.009 ns | 1.122 ns | 48.87 ns |  1.10 |    0.04 | 0.0032 |      40 B |          NA |

| Method               | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Guid.Parse           | 17.85 ns | 0.384 ns | 1.046 ns | 17.35 ns |  1.00 |    0.08 |      - |         - |          NA |
| Ulid.ParseString     | 58.50 ns | 0.772 ns | 0.722 ns | 58.39 ns |  3.29 |    0.18 | 0.0032 |      40 B |          NA |
| Ulid.ParseUtf8String | 59.45 ns | 1.093 ns | 1.023 ns | 59.13 ns |  3.34 |    0.19 | 0.0032 |      40 B |          NA |

| Method        | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Guid.ToString | 11.39 ns | 0.271 ns | 0.240 ns |  1.00 |    0.03 | 0.0076 |      96 B |        1.00 |
| Ulid.ToString | 30.13 ns | 0.603 ns | 1.324 ns |  2.65 |    0.13 | 0.0063 |      80 B |        0.83 |

Legend:
  - Mean      : Arithmetic mean of all measurements
  - Error     : Half of 99.9% confidence interval
  - StdDev    : Standard deviation of all measurements
  - Ratio     : Mean of the ratio distribution ([Current]/[Baseline])
  - RatioSD   : Standard deviation of the ratio distribution ([Current]/[Baseline])
  - Gen0      : GC Generation 0 collects per 1000 operations
  - Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  - 1 ns      : 1 Nanosecond (0.000000001 sec)

[^1] `Guid.NewGuid` is ~9 times slower than `Ulid.NewUlid` because it uses a cryptographic random number generator on every
call, whereas `Ulid.NewUlid` only uses it when the millisecond timestamp changes and if it doesn't, it just increments the
previous random part.
