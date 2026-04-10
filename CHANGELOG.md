# Changelog


## v3.0.3-preview.1 - 2026-04-11

### Internal

- update CHANGELOG to stick to common formatting for breaking changes
- update CHANGELOG to reflect removal of inconsistent static methods from Ulid struct
- update vm2.TestUtilities to version 1.4.0 and inherit test classes from TestBase
- UlidTests inherit from TestBase, update CHANGELOG for breaking changes in Ulid struct and improve formatting



## v3.0.2 - 2026-04-10
See prereleases below.

## v3.0.1-preview.2 - 2026-04-10
See prereleases below.
## v3.0.1-preview.2 - 2026-04-10

### Internal

- clean up CHANGELOG formatting and update cliff.prerelease.toml template


## v3.0.1 - 2026-04-10

### Internal

- curate changelog for v2.0.1-preview.1 through v3.0.0
- update changelog for v3.0.1 [skip ci]

## v3.0.1 - 2026-04-10

See prereleases below.

## v3.0.1-preview.1 - 2026-04-10

### Fixed

- curate CHANGELOG and fix git-cliff template for v2.x

## v3.0.0 - 2026-04-10

See prereleases below.

## v3.0.0-preview.1 - 2026-04-10

### Changed

- **BREAKING:** Renamed namespace `vm2.Serialization` to `vm2.UlidSerialization`
- **BREAKING:** Changed implicit `string` conversion to explicit for `Ulid`

### Removed

- **BREAKING:** Removed `IEqualityComparer<Ulid>` implementation from `Ulid`

### Internal

- Update copyright year to 2025-2026
- Add JSON and Markdown exporters and `MemoryDiagnoser` to benchmark classes
- Update CI workflows, package dependencies, and vm2.TestUtilities to 1.3.1

## v2.1.1-preview.1 - 2026-03-25

### Internal

- Clean up formatting in CHANGELOG.md

## v2.1.0 - 2026-03-24

See prereleases below.

## v2.1.0-preview.2 - 2026-03-24

### Internal

- Update workflow files to remove default values for environment variables

## v2.1.0-preview.1 - 2026-03-24

### Internal

- Update CI, Prerelease, and Release workflows for consistency
- Update vm2.TestUtilities to 1.3.0 and package dependencies to 10.0.5
- Add auto-merge workflow and Dependabot improvements

## v2.0.1 - 2026-03-08

See prereleases below.

## v2.0.1-preview.1 - 2026-03-08

### Internal

- Add security category to changelog configuration
- Streamline CHANGELOG.md updates

## v2.0.0 - 2026-03-08

See prereleases v2.0.0-preview.1 and v2.0.0-preview.2 below for full details.

### Summary

- **BREAKING:** Removed `GetTimestampFromUlid`, `PutTimestampToUlid`, and optional parameter from `NewUlid`.

  This is a breaking change because any code that called these methods or used the optional parameter will no longer compile.
  Users will need to update their code to use the new API for working with timestamps and generating ULIDs.

- **BREAKING:** Removed `TryWrite(Span<byte>, bool)` in favor of `TryWriteUtf8(Span<byte>)`.

  This is a breaking change because any code that called the old `TryWrite` method with the `asUtf8` parameter will no longer
  compile. Users will need to update their code to call the new `TryWriteUtf8` method instead.

- Added UTF-8 parse/write overloads, performance improvements.

## v2.0.0-preview.2 - 2026-03-08

### Internal

DevOps changes only.

## v2.0.0-preview.1 - 2026-02-24

### Removed

- **BREAKING:** removed the static methods `GetTimestampFromUlid(in ReadOnlySpan<byte> ulidBytes)` and
  `PutTimestampToUlid(in DateTime timestamp, Span<byte> ulidBytes)` from the `Ulid` struct, as they were not consistent with
  the rest of the API and had confusing semantics.

  This is a breaking change because any code that called these methods will no longer compile. If users need to get or put
  timestamps in ULIDs, they can use the `UlidFactory` class with the appropriate providers instead.

- **BREAKING:** removed the parameter of the static method `Ulid.NewUlid(/*IUlidRandomProvider?
  ulidRandomProvider = null*/)`, as it had confusing side effects and was incomplete: it accepted a random provider but no
  timestamp provider. Now, the method simply generates a new ULID using the default random and timestamp providers.

  This is a breaking change because any code that called `Ulid.NewUlid()` with a custom random provider will no longer compile.
  If users need to use a custom random provider, they can create an instance of `UlidFactory` with the desired providers and
  call `factory.NewUlid()` instead.

- **BREAKING:** removed the method `public readonly bool TryWrite(Span<byte> destination, bool asUtf8)`. Use
  `public readonly bool TryWriteUtf8(in Span<byte> destination)` instead, to clarify the semantics of writing ULIDs as UTF-8
  encoded byte spans.

### Added

Added new overloads `Ulid.Parse(ReadOnlySpan<byte> utf8Bytes)` and `Ulid.TryParse(ReadOnlySpan<byte> utf8Bytes, out Ulid result)
` to parse ULIDs from UTF-8 encoded byte spans. The existing `Parse` and `TryParse` methods that take `ReadOnlySpan<char>` are
still available and unchanged.

### Performance

Some performance improvements in **`parse`** and **`write`** families of methods.

## v1.0.9 - 2026-02-16

### Internal

DevOps build pipeline changes.

## v1.0.8 - 2026-02-14

### Added

UlidTool project to provide a command-line interface for generating and parsing ULIDs. The tool is built on top of the `vm2.
Ulid` library and can be used for quick ULID generation or parsing without writing code.

### Internal

Build pipeline changes to include the new UlidTool project in the CI process and package it for release.

## v1.0.7 - 2026-02-13

### Changed

Dependencies to .NET SDK 10.0.3 and latest build pipeline templates from vm2.DevOps.

## v1.0.6 - 2026-02-09

### Internal

DevOps build pipeline changes

## v1.0.5 - 2026-02-09

### Internal

DevOps build pipeline changes

## v1.0.4 - 2026-01-02

### Added

- IClock interface and SystemClock implementation to get the current time used by UlidFactory (testability!).
- JSON serializers for both `System.Text.Json` and `Newtonsoft.Json`
- Method `public readonly bool TryWriteUtf8(in Span<byte> destination)` (see also [Changed](#changed) below)
- Implicit conversion to/from `string` and `Guid`
- Unit tests for the new features above and the fixed bug below.

### Changed

Small API changes that clarify the semantic of some input parameters:

- Change the constructor `public Ulid(in ReadOnlySpan<byte> bytes)` to `public Ulid(in ReadOnlySpan<byte> bytes, bool isUtf8)`.
  The constructor used to guess whether the input is raw bytes or UTF-8 sequence of characters by the length of the parameter
  `bytes`. Now, let the caller state their intention explicitly.
- Similar change for `public readonly bool TryWrite(Span<byte> destination, bool asUtf8)` - added the explicit parameter
  `asUtf8`.
- Keeping the semantics of the `Parse` and `TryParse` methods: always parsing either UTF-16 characters (`ReadOnlySpan<char>`)
  or UTF-8 characters (`ReadOnlySpan<byte>`).
- Minor stylistic code changes.

### Fixed

- Fixed bug where the UlidFactory could produce non-monotonic ULIDs when called within the same millisecond and the last byte
  of the previous ULID was `0xFF`.

### Performance

- Small optimization of `UlidToString()`: Using the new `string.Create` (thank you Stephen Toub!).

### Internal

DevOps build pipeline changes

## v1.0.3 - Skipped

## v1.0.2 - 2025-09-19

### Changed

- Suppress creation of packages for the example project(s).

## v1.0.1 - 2025-09-19

### Changed

- Changed the package name from `vm2.Ulid` to `Vm.Ulid`.
- Changed also in the README.md and other documentation files.

## v1.0.0 - 2025-09-19

- The initial version.

## Usage Notes

> [!TIP] Be disciplined with your commit messages and let git-cliff do the work of updating this file.
>
> **Added:**
>
> - add new features here
> - commit prefix for git-cliff: `feat:`
>
> **Changed:**
>
> - add behavior changes here
> - commit prefix for git-cliff: `refactor:`
>
> **Fixed:**
>
> - add bug fixes here
> - commit prefix for git-cliff: `fix:`
>
> **Performance**
>
> - add performance improvements here
> - commit prefix for git-cliff: `perf:`
>
> **Removed**
>
> - add removed/obsolete items here
> - commit prefix for git-cliff: `revert:`
>
> **Security**
>
> - add security-related changes here
> - commit prefix for git-cliff: `security:`
>
> **Internal**
>
> - add internal changes here
> - commit prefix for git-cliff: `refactor:`, `docs:`, `style:`, `test:`, `chore:`, `ci:`, `build:`
>

## References

This format follows:

- [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
- [Semantic Versioning](https://semver.org/)
- Version numbers are produced by [MinVer](./ReleaseProcess.md) from Git tags.
