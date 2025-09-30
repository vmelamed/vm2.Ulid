# GitHub Copilot Instructions – vm2.Ulid

> Purpose: Give AI assistants precise, low-noise guidance so generated changes align with this repository’s design, performance, testing, and release conventions.

## 1. Project Overview
* Library: High‑performance .NET (C# 12 / .NET 9) implementation of ULID (Universally Unique Lexicographically Sortable Identifier).
* Core type: `readonly partial struct Ulid` (immutable, 16 bytes logically, wraps a 16‑byte backing memory block).
* Goals: Allocation minimization, branch clarity, predictable ordering (timestamp prefix, monotonic random increment inside same millisecond), thread safety in `UlidFactory`.
* Secondary assets: BenchmarkDotNet benchmarks, xUnit tests, Bash & PowerShell automation scripts, GitHub Actions workflows (CI, prerelease, release, shell linting, coverage, benchmarks).

## 2. Core Design Invariants
When generating or editing code touching ULIDs, DO NOT violate:
1. ULID layout: 6 timestamp bytes (big‑endian milliseconds) + 10 random bytes.
2. String form: Exactly 26 Crockford Base32 chars (alphabet: `0123456789ABCDEFGHJKMNPQRSTVWXYZ`). Case-insensitive input, uppercase output.
3. `TryParse` must be length & alphabet validating; must not throw – only return false.
4. `Parse` / constructor overloads wrap `TryParse` & throw `ArgumentException` on failure.
5. `UlidFactory` monotonic guarantee: same millisecond => increment random tail with carry; overflow => `OverflowException`.
6. Increment operator (`++`) performs unsigned byte-wise increment with overflow detection (throws at all bits set).
7. All public APIs must remain allocation-conscious (prefer stackalloc & spans, avoid LINQ in hot paths, no boxing, no unnecessary string interpolation inside loops).
8. Hash code & ordering semantics based strictly on byte sequence (lexicographic).
9. `ToString()` must produce canonical uppercase output; `TryWrite` variants must respect destination size before writing.
10. Random provider and clock are injected abstractions (`IUlidRandomProvider`, `IClock`) for testability & determinism.

## 3. Performance & Memory Guidelines
* Favor `Span<T>` / `ReadOnlySpan<T>` over allocating arrays; only allocate when external ownership is required.
* Use `stackalloc` for small fixed buffers (≤ 128 bytes) in parsing/formatting.
* Avoid exception usage in normal control flow; guard early and return fast.
* Minimize branches inside the 26‑iteration encode/decode loops.
* Do not introduce culture‑sensitive operations; always ordinal / invariant.
* Keep hot path methods `static` or `readonly` instance; prefer local variables over repeated property calls.
* Avoid reflection, dynamic, LINQ, regex (parsing implemented manually for speed).

## 4. Thread Safety & Concurrency
* `Ulid` struct is immutable → no internal mutation after construction.
* `UlidFactory` uses a private lock for the critical section updating `_lastRandom` & `_lastTimestamp`; do not expose internal mutable state.
* Do not add static mutable global state except cached readonly lookup tables.

## 5. API Evolution Rules
* Maintain backward compatibility (no breaking rename/removal of public members without justification).
* New parsing / formatting helpers must have non-alloc `Try...` form first.
* Avoid widening implicit conversions. Existing implicit conversions: `Ulid ↔ Guid`, `Ulid ↔ string` (string → parse). Do not add risky implicit conversions (e.g., to integral types) – prefer explicit named methods.
* Keep constructor overload semantics consistent: throwing vs non‑throwing `Try...` patterns.
* If adding serialization integration (e.g., System.Text.Json source gen), gate it behind partial files & optional features.

## 6. Error Handling Patterns
* Validate buffer length up front; throw `ArgumentException` with clear parameter names.
* Use `ArgumentException` (not `FormatException`) for invalid ULID textual sources (consistent with current code).
* Overflow scenarios (increment past max or random overflow inside same millisecond) use `OverflowException`.
* Never swallow exceptions silently in core library; only catch if rethrowing with added context.

## 7. Parsing / Encoding Details
* Parsing loops accumulate into `UInt128` (big‑endian). Each digit multiplies prior by radix (32) then adds digit value.
* Mapping from char → digit uses precomputed `CrockfordDigitValues` table; maintain sentinel 255 for invalid entries.
* Encoding extracts least-significant 5 bits iteratively (mask `0x1F`), writing backwards from end → start.
* Maintain constant `UlidStringLength = ceil(128 / 5) = 26`.
* Keep code path symmetrical for UTF-16 (`ReadOnlySpan<char>`) & UTF-8 (`ReadOnlySpan<byte>`).

## 8. Testing Conventions
* Framework: xUnit, FluentAssertions.
* File: `UlidTests.cs` centralizes structural, parsing, monotonicity & overflow tests.
* Add new tests alongside existing grouping – prefer TheoryData for cross-case enumerations.
* For any new branch or error condition, add at least one failing case test & one success case test.
* Randomness tests should avoid probabilistic flakiness – inject deterministic providers.
* Keep tests allocation-light; avoid long loops unless benchmarking.

## 9. Benchmarking Guidelines
* Benchmarks are in `benchmarks/UlidType.Benchmarks` (BenchmarkDotNet).
* When adding new performance-sensitive API, create a micro-benchmark measuring encode, parse, or factory throughput.
* CI collects JSON results; jq script aggregates medians. Maintain result schema compatibility (`BenchmarkDotNet` standard fields).
* Avoid gratuitous changes that rename benchmark methods without updating jq summarization logic.

## 10. Versioning & Release
* Versioning: MinVer, tag prefix `v` (e.g., `v1.0.4`). Pre-release tags include hyphen segments (e.g., `v1.0.5-alpha.1`).
* Release workflow expects annotated or lightweight git tag creation; `release.yml` publishes to NuGet.
* Do not manually edit `<Version>` in project file; rely on tags + MinVer configuration.

## 11. Coding Style (C#)
* Namespaces: `vm2` root; avoid nested deep hierarchies for small surface.
* Access modifiers explicit (`public`, `internal`, etc.).
* Favor expression-bodied members for short methods where readability not reduced.
* Use `readonly` where possible on structs & fields to make intent & immutability explicit.
* Put XML docs on all public members; maintain param names in doc tags.
* Keep constants & lookup tables in `Ulid.Constants.cs`; avoid scattering magic numbers.
* Keep region use modest – only for implementing interface groupings (as current pattern).

## 12. Bash / PowerShell Automation Rules
* Bash scripts live under `scripts/bash`; PowerShell equivalents under `scripts/pwsh`.
* Scripts should be POSIX‑leaning but can use Bash‑specific features (arrays, `[[ ... ]]`).
* Continue converging on output-over-global-return variable (planned refactor) – for now, existing `return_*` conventions remain.
* Always quote variable expansions unless intentionally word-splitting.
* For new prompts: respect `quiet` mode (auto default) & `dry_run` logic in `execute`.
* Self-tests: extend `scripts/bash/self-test.sh` or `scripts/pwsh/Self-Test.ps1` when adding reusable functions.
* If adding dependencies (e.g., `yq`), ensure graceful skip if not installed.

## 13. GitHub Actions & CI Expectations
* CI must stay green: build, test, coverage, benchmarks summarization, shell linting.
* Do not add long-running (>5 min) steps without justification.
* Coverage threshold enforced via scripts; update threshold only with rationale.
* When adding a new project: ensure it’s included in solution & restore/build/test cycle.

## 14. Security & Safety
* No network I/O in core library APIs (deterministic local logic only).
* Randomness sources must be explicitly chosen (`CryptoRandom` for cryptographic, `PseudoRandom` for non‑critical scenarios).
* Avoid unsafe code blocks unless a measurable performance gain is proven and benchmarked.
* Do not log sensitive data (no logging layer currently present; keep it that way unless requirements change).

## 15. Adding New Features – Checklist
When proposing a new feature (e.g., custom ULID format, alternative encoding):
1. Define invariants & performance target (baseline vs new approach) in PR description.
2. Add micro-benchmark comparing old/new (if performance-related).
3. Add unit tests (success + failure cases + edge boundary).
4. Maintain backward compatibility; feature gate if experimental.
5. Update this instructions file if conventions shift.

## 16. Common Pitfalls to Avoid
* Returning newly allocated `byte[]` repeatedly in hot loops – use spans.
* Introducing upper/lower casing conversions in encode loop (output is already uppercase via digits table).
* Using regex to validate ULID before parse – redundant & slower.
* Forgetting to clamp / check destination buffer size in `TryWrite` calls.
* Modifying `UlidFactory` without considering multi-thread race conditions.
* Adding LINQ / `foreach` in parse/encode loops (prefer counted `for`).

## 17. Example Extension Patterns
* Add a `TryFormat` variant: mirror `TryWrite` style: check length → encode → return bool.
* Add `TryParse(ReadOnlySpan<char>, Span<byte> rawBytes)` if exporting raw bytes directly – maintain early exit on invalid char.
* Introduce new random provider: implement `IUlidRandomProvider.Fill` thread-safe; no internal synchronization if stateless.

## 18. Benchmarks – Adding a Case
Minimal template (place in `UlidBenchmarks.cs`):
```csharp
[Benchmark]
public void Parse_Existing() {
    Ulid.TryParse(_sampleSpan, out _);
}
```
Ensure `_sampleSpan` prepared in `[GlobalSetup]` to avoid per-iteration allocation.

## 19. Documentation & Comments
* Keep XML docs precise, technical, and performance-relevant.
* Update remarks when changing complexity or algorithmic approach.
* Avoid oversharing implementation rationale in summary – put deeper context in remarks.

## 20. PR Review Quick Rubric (AI Self-Check)
Before finalizing a generated PR, ensure:
* [ ] No new allocations in hot paths (encode/parse/factory) without benchmark justification.
* [ ] All new public members documented.
* [ ] Tests compile & pass locally for added scenarios.
* [ ] Coverage does not drop below threshold.
* [ ] Benchmarks still run (schema unchanged) or jq updated accordingly.
* [ ] No accidental style drift (e.g., inconsistent spacing, unused usings).
* [ ] Version not manually set in project file.

## 21. When NOT to Autogenerate Code
* Cryptographic algorithm changes.
* Subtle memory layout changes (would risk ABI / serialization expectations).
* Large refactors merging unrelated concerns.

## 22. Contact / Escalation
If an AI tool is uncertain about a structural change (e.g., altering public API or performance-critical internal loops), it should: (a) open a draft PR with rationale; (b) request human confirmation before merging.

---
Generated & curated for this repository. Update sections pragmatically when conventions evolve.
