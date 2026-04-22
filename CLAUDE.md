# vm2.Ulid — Claude Context

@~/.claude/CLAUDE.md
@~/repos/vm2/CLAUDE.md
@.github/CONVENTIONS.md

## Package Identity

- Repo: <https://github.com/vmelamed/vm2.Ulid>
- NuGet: <https://www.nuget.org/packages/vm2.Ulid/>
- Status: Published, stable
- Target: .NET 10.0+
- Implements: ULID specification (<https://github.com/ulid/spec>)

## What This Package Does

Provides `vm2.Ulid` — a spec-compliant, fast, allocation-efficient ULID value type
for .NET — and `vm2.UlidFactory` for monotonic, thread-safe generation.

Key design decisions:

- `Ulid` is a `readonly record struct` (128-bit value, no heap allocation)
- `UlidFactory` tracks timestamp and last random bytes for same-millisecond monotonicity
- Randomness and timestamp are injected via `IRandomNumberGenerator` and
  `ITimestampProvider` — testable, DI-friendly
- `Ulid.NewUlid()` static method uses an internal singleton factory (convenience API)
- Full `Guid` interop: `ToGuid()` and `new Ulid(guid)`
- UTF-8 and UTF-16 parsing supported
- Companion CLI tool: vm2.UlidTool

## Repository Layout

```text
vm2.Ulid/
├── src/
│   ├── UlidType/         # Core library: Ulid struct, UlidFactory, providers
│   └── UlidTool/         # CLI tool: generate ULIDs from command line
├── test/
│   └── UlidType.Tests/   # xUnit v3 tests
├── benchmarks/
│   └── UlidType.Benchmarks/
├── examples/
│   └── GenerateUlids.cs  # Runnable script example
└── vm2.Ulid.slnx
```

## Performance Characteristics

- `Factory.NewUlid()` and `Ulid.NewUlid()`: ~63 ns (vs `Guid.NewGuid()`: ~591 ns)
- ~9x faster than `Guid.NewGuid()` with CryptoRandom
- Monotonicity: increments random part within same millisecond instead of
  calling the RNG again — key to the performance advantage
- `Ulid.ToString()`: ~60 ns, ~3.5x slower than `Guid.ToString()` (known trade-off)

## Known Trade-offs and Design Notes

- `ToString()` is slower than `Guid.ToString()` — acceptable given generation speed
- Multiple `UlidFactory` instances recommended per context (e.g. one per entity type)
  for independent monotonicity domains
- Global monotonicity in distributed systems requires a centralized ULID service
  or consensus — node-local monotonicity does not imply global monotonicity
- Clock skew is a real concern in distributed generation

## Active Work / Known Issues

- [Fill in when working on this package]

## Prompting Notes for This Package

- When working on `Ulid` struct internals: note it is a `readonly record struct`;
  all operations must be non-mutating.
- When working on `UlidFactory`: the monotonicity invariant is the core correctness
  concern — any change must preserve it under concurrent access.
- When writing tests: always inject `ITimestampProvider` and `IRandomNumberGenerator`
  for determinism. Never rely on live clock or RNG in unit tests.
- Benchmarks use BenchmarkDotNet; always run in Release configuration.
