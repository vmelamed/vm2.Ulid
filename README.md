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

| Method               | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | RandomProviderType
|--------------------- |----------:|---------:|---------:|------:|-------:|----------:|:-------------------
| UlidFactory.NewUlid  |  59.10 ns | 0.409 ns | 0.362 ns |  0.10 | 0.0024 |      40 B | CryptoRandom
| Ulid.NewUlid         |  59.54 ns | 0.179 ns | 0.168 ns |  0.10 | 0.0024 |      40 B | CryptoRandom
| Guid.NewGuid [^1]    | 595.81 ns | 1.776 ns | 1.575 ns |  1.00 |      - |         - | CryptoRandom
|                      |           |          |          |       |        |           |
| UlidFactory.NewUlid  |  58.84 ns | 0.175 ns | 0.155 ns |  0.10 | 0.0024 |      40 B | PseudoRandom
| Ulid.NewUlid         |  59.42 ns | 0.234 ns | 0.207 ns |  0.10 | 0.0024 |      40 B | PseudoRandom
| Guid.NewGuid [^1]    | 593.93 ns | 1.612 ns | 1.429 ns |  1.00 |      - |         - | PseudoRandom
|                      |           |          |          |       |        |           |
| Guid.Parse           |  30.78 ns | 0.111 ns | 0.099 ns |  1.00 |      - |         - |
| Ulid.ParseString     |  70.88 ns | 0.499 ns | 0.443 ns |  2.30 | 0.0048 |      80 B |
| Ulid.ParseUtf8String |  72.22 ns | 1.457 ns | 1.496 ns |  2.35 | 0.0048 |      80 B |
|                      |           |          |          |       |        |           |
| Guid.ToString        |  16.16 ns | 0.158 ns | 0.148 ns |  1.00 | 0.0057 |      96 B |
| Ulid.ToString        |  49.07 ns | 0.517 ns | 0.484 ns |  3.04 | 0.0048 |      80 B |

Legend:
  - Mean      : Arithmetic mean of all measurements
  - Error     : Half of 99.9% confidence interval
  - StdDev    : Standard deviation of all measurements
  - Ratio     : Mean of the ratio distribution ([Current]/[Baseline])
  - RatioSD   : Standard deviation of the ratio distribution ([Current]/[Baseline])
  - Gen0      : GC Generation 0 collects per 1000 operations
  - Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  - 1 ns      : 1 Nanosecond (0.000000001 sec)

[^1] `Guid.NewGuid` is ~10 times slower than `Ulid.NewUlid` because it uses a cryptographic random number generator on every
call, whereas `Ulid.NewUlid` only uses it when the millisecond timestamp changes and if it doesn't, it just increments the
previous random part.
