# Copilot Instructions for vm2.Ulid

## Shared Conventions

Copilot MUST read and follow [CONVENTIONS.md](CONVENTIONS.md) before suggesting or making changes.

Do not duplicate shared rules here — shared instructions belong in [CONVENTIONS.md](CONVENTIONS.md) so all AI systems
use the same source of truth.

## Package-Specific Guidance

- .NET package that implements [Universally Unique Lexicographically Sortable Identifier (ULID)](https://github.com/ulid/spec).
- The package exposes a `vm2.Ulid` value type and a `vm2.UlidFactory` for stable, monotonic generation.
- ULIDs must increase monotonically within the same millisecond. When multiple ULIDs are generated in a single millisecond, each subsequent ULID is greater by one in the least significant byte(s). A ULID factory tracks the timestamp and the last random bytes for each call. When the timestamp matches the previous generation, the factory increments the prior random part instead of generating a new random value.
- The `vm2.UlidFactory` class encapsulates the requirements and exposes a simple interface for generating ULIDs. Use multiple `vm2.UlidFactory` instances when needed, e.g. one per aggregate root or database table.
- ULID factories MUST be thread-safe and ensure monotonicity of generated ULIDs across application contexts. The factory uses two providers: one for the random bytes and one for the timestamp.
- Use dependency injection to construct the factory and manage the providers. DI keeps the provider lifetimes explicit, makes testing simple, and enforces a single, consistent configuration across the app or service.
- In simple scenarios, use the static method `vm2.Ulid.NewUlid()` instead of `vm2.UlidFactory`. It uses an internal single static factory instance with a cryptographic random number generator and a clock based on `System.TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()`.
- By default the `vm2.UlidFactory` uses a thread-safe, cryptographic random number generator (`vm2.UlidRandomProviders.CryptoRandom`), which is suitable for most applications. If you need a different source of randomness, e.g. for testing purposes, for performance reasons, or if you are concerned about your source of entropy (`/dev/random`), you can explicitly specify that the factory should use the  pseudo-random number generator `vm2.UlidRandomProviders.PseudoRandom`. You can also provide your own, thread-safe implementation of `vm2.IRandomNumberGenerator` to the factory.
- By default, the timestamp provider uses `System.TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()` converted to Unix epoch time in milliseconds. If you need a different source of time, e.g. for testing purposes, you can use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` (package `Microsoft.Extensions.TimeProvider.Testing`) or provide your own implementation that overrides the .NET BCL class `System.TimeProvider` and pass that to the factory.
- The `vm2.Ulid` type is marked with the `System.Text.Json.Serialization.JsonConverterAttribute` attribute, so it can be serialized and deserialized by `System.Text.Json` without any additional  configuration. For Newtonsoft.Json, use the companion package `vm2.UlidSerialization.NsJson` ([source code](https://github.com/vmelamed/vm2.Ulid/blob/main/src/UlidNsConverter)).
