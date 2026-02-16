# Changelog



## v1.0.8 - 2026-02-16
See prereleases below.



See prereleases below. This format follows:

- [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
- [Semantic Versioning](https://semver.org/)
- Version numbers are produced by [MinVer](./ReleaseProcess.md) from Git tags.

<!--
## [Unreleased]

### Added
- (add new features here)

### Changed
- (add behavior changes here)

### Fixed
- (add bug fixes here)

### Performance
- (add performance improvements here)

### Removed
- (add removed/obsolete items here)

### Security
- (add security-related changes here)

### Internal
- (tooling, infrastructure, build pipeline changes)
-->

---

## v1.0.7 - 2026-02-13

Update dependencies to .NET SDK 10.0.3 and latest build pipeline templates from vm2.DevOps.

## v1.0.6 - 2026-02-09

See prereleases below.

## [1.0.6] - 2026-02-09

### Internal

- build pipeline changes

---

## [1.0.5] - 2026-02-09

- build pipeline changes

## [1.0.4] - 2026-01-02

- Moved to the latest build pipeline templates from vm2.DevOps.

### Added

- IClock interface and SystemClock implementation to get the current time used by UlidFactory (testsbility!).
- JSON serializers for both `System.Text.Json` and `Newtonsoft.Json`
- Method `public readonly bool TryWriteUtf8(in Span<byte> destination)` (see also [Changed](#changed) below)
- Implicit conversion to/from `string` and `Guid`
- Unit tests for the new features above and the fixed bug below.
- A lot of CI/CD workflow improvements and scripts to automate the release process (see also [Internal](#internal) below).

### Changed

Small API changes that clarify the semantic of some input parameters:

- Change the constructor `public Ulid(in ReadOnlySpan<byte> bytes)` to `public Ulid(in ReadOnlySpan<byte> bytes, bool isUtf8)`.
  The constructor used to guess whether the input is raw bytes or UTF-8 sequence of characters by the length of the parameter
  `bytes`. Now, let the caller state their intention explicitly.
- Similar change for `public readonly bool TryWrite(Span<byte> destination, bool asUtf8)` - added the explicit parameter
  `asUtf8`.
- Keeping the semantics of the `Parse` and `TryParse` methods: always parsing either UTF-16 characters (`ReadOnlySpan<char>`) or
  UTF-8 characters (`ReadOnlySpan<byte>`).
- Minor stylistic code changes.

### Fixed

- Fixed bug where the UlidFactory could produce non-monotonic ULIDs when called within the same millisecond and the last byte of
  the previous ULID was `0xFF`.

### Performance

- Small optimization of `UlidToString()`: Using the new `string.Create` (thank you Stephen Toub!).
- build pipeline changes

---

## [1.0.3] - Skipped

---

## [1.0.2] - 2025-09-19

### Changed

- Suppress creation of packages for the example project(s).

---

## [1.0.1] - 2025-09-19

### Changed

- Changed the package name from `vm2.Ulid` to `Vm.Ulid`.
- Changed also in the README.md and other documentation files.

## [1.0.0] - 2025-09-19

- The initial version.

---

## Usage Notes

1. For every change, prefer small bullet points written in the imperative mood (e.g. "Add …", "Fix …").
2. Group changes under the appropriate heading above; add new headings only when needed.
3. Before creating a stable tag:
   - Move items from `Unreleased` into a new `## [vX.Y.Z] - YYYY-MM-DD` section.
   - Update the comparison links at the bottom if a major/minor line starts a new baseline.
4. Do not rewrite history of published versions—append corrections in a new entry if needed.

## Categorization Guidance

| Category     | Use for                                                            |
|--------------|--------------------------------------------------------------------|
| Added        | New public APIs, features, options                                 |
| Changed      | Backward-impacting behavior changes (document clearly)             |
| Fixed        | Bug fixes                                                          |
| Performance  | Measurable speed / memory improvements                             |
| Removed      | Deprecated APIs removed; breaking removals                         |
| Security     | Vulnerability fixes, hardening, dependency CVE responses           |
| Internal     | Build, CI, tooling, refactors without API / behavior change        |

## After Tagging

When you create a stable tag (e.g. `v1.2.0`):

1. Add the dated section.
2. Adjust `[Unreleased]` link to compare new tag to `HEAD`.
3. Add a new link definition for the released version.

---

## Link References

(Adjust initial tag if your first stable differs.)
