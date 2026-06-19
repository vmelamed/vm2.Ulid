# Copilot Instructions for vm2.Ulid

## Shared Conventions

Copilot MUST read and follow [CONVENTIONS.md](CONVENTIONS.md) before suggesting or making changes.

Do not duplicate shared rules here — shared instructions belong in [CONVENTIONS.md](CONVENTIONS.md) so all AI systems
use the same source of truth.

## What This Package Does

Implements [Universally Unique Lexicographically Sortable Identifier (ULID)](https://github.com/ulid/spec). Provides `vm2.Ulid` — a spec-compliant, fast, allocation-efficient ULID value type for .NET — and `vm2.UlidFactory` for monotonic, thread-safe generation.

### Key Design Decisions

- `Ulid` is a `readonly struct` (128-bit value)
- `UlidFactory` tracks timestamp and last random bytes for same-millisecond monotonicity
- Randomness and timestamp are injected via `IUlidRandomProvider` and `System.TimeProvider` — testable, DI-friendly
- `Ulid.NewUlid()` static method uses an internal singleton factory (convenience API)
- Full `Guid` interop: `ToGuid()` and `new Ulid(guid)`
- UTF-8 and UTF-16 parsing supported
- Companion CLI tool: vm2.UlidTool
- The `vm2.UlidFactory` class encapsulates the requirements and exposes a simple interface for generating ULIDs. Use multiple `vm2.UlidFactory` instances when needed, e.g. one per aggregate root or database table.
- ULID factories MUST be thread-safe and ensure monotonicity of generated ULIDs across application contexts. The factory uses two providers: one for the random bytes and one for the timestamp.
- Use dependency injection to construct the factory and manage the providers. DI keeps the provider lifetimes explicit, makes testing simple, and enforces a single, consistent configuration across the app or service.
- In simple scenarios, use the static method `vm2.Ulid.NewUlid()` instead of `vm2.UlidFactory`. It uses an internal single static factory instance with a cryptographic random number generator and a clock based on `System.TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()`.
- By default the `vm2.UlidFactory` uses a thread-safe, cryptographic random number generator (`vm2.Providers.Ulid.CryptoRandom`). For a different source of randomness, use the  pseudo-random number generator `vm2.Providers.Ulid.PseudoRandom` or provide your own implementation of `vm2.IUlidRandomProvider` to the factory.
- By default, the timestamp provider uses `System.TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()` converted to Unix epoch time in milliseconds. For a different source of time, e.g. use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` (package `Microsoft.Extensions.TimeProvider.Testing`) or provide implementation that overrides `System.TimeProvider`.
- The `vm2.Ulid` type is marked with the `System.Text.Json.Serialization.JsonConverterAttribute`. For Newtonsoft.Json, use [the companion package](https://www.nuget.org/packages/vm2.Ulid.Serialization.NsJson/) with  ([source code](https://github.com/vmelamed/vm2.Ulid/blob/main/src/Serialization.NsJson.Ulid/)).

## Common Local Commands

```bash
# Build
dotnet build vm2.Ulid.slnx

# Run tests (xUnit v3, MTP v2 — each project is a compiled executable)
dotnet test --project tests/Ulid/Ulid.Tests.csproj

# Run test executables (xUnit v3, MTP v2 — each project is a compiled to an executable) on Linux:
tests/Ulid/bin/Debug/net10.0/Ulid.Tests # or
tests/Ulid/bin/Debug/net10.0/Ulid.Tests.exe #  on Windows

# Run a single test by method name (xUnit v3, MTP v2 filter syntax)
dotnet test --project tests/Ulid/Ulid.Tests.csproj --filter "MethodName_WhenCondition_ShouldOutcome"

# Pack NuGet package
dotnet pack vm2.Ulid.slnx --configuration Release

# Run benchmarks (Release only)
dotnet run --project benchmarks/Ulid/Ulid.Benchmarks.csproj --configuration Release -- --filter "*"

# If the benchmark tests are already built, you can run the compiled executable directly:
benchmarks/Ulid/bin/Release/net10.0/Ulid.Benchmarks --help
benchmarks/Ulid/bin/Release/net10.0/Ulid.Benchmarks --filter "*" # on Linux
benchmarks/Ulid/bin/Release/net10.0/Ulid.Benchmarks.exe --filter "*" # on Windows
```

Tests use MTP v2 (Microsoft Testing Platform v2) with xUnit v3 — they compile to standalone executables.
Use `dotnet test --project <path>` per project; solution-wide `dotnet test` is not supported with MTP v2.

## Performance Characteristics

Performance numbers:

See [the performance section in the README](https://github.com/vmelamed/vm2.Ulid#performance) for detailed benchmark results.

## Known Trade-offs and Design Notes

- `ToString()` is slower than `Guid.ToString()` — acceptable given generation speed
- Multiple `UlidFactory` instances recommended per context (e.g. one per entity type)
  for independent monotonicity domains
- Global monotonicity in distributed systems requires a centralized ULID service
  or consensus — node-local monotonicity does not imply global monotonicity
- Clock skew is a real concern in distributed generation

## Active Work / Known Issues

- Internally the Ulid is stored as a 128-bit value. It is a `ReadOnlyMemory<byte>` wrapping a `byte[16]` array - a reference type wrapper. Is this the most efficient way. Could a value based `ulong low; ulong high` or `Vector128<byte>` provide better performance or memory layout? Can the performance of the Ulid generation and conversions to/from string be improved by using a different internal representation?

## Prompting Notes for This Package

- When working on `Ulid` struct internals: note it is a `readonly struct`; all operations must be non-mutating.
- When working on `UlidFactory`: the monotonicity invariant is the core correctness concern — any change must preserve it under concurrent access.
- When writing tests: always inject `System.TimeProvider` and `IUlidRandomProvider` in the provider instances for determinism. Never rely on live clock or RNG in unit tests.
- Benchmarks use BenchmarkDotNet; always run in Release configuration.
