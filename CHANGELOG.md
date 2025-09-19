# Changelog

All notable changes to this project will be documented in this file.

This format follows:
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

## [v0.1.2] - 2025-09-19

### Changed
- Minor document fixes

### Performance
- Better performance for `Ulid.NewUlid`, `Ulid.Parse`, `Ulid.ToString`, and the related methods.

---

## [v0.1.1] - 2025-09-17

Initial baseline release.

### Added

- Core library foundation.
- Initial tests & benchmarking harness.
- MinVer-based versioning & automated prerelease workflow.
- Release process documentation (`ReleaseProcess.md`).
- SourceLink, symbols (`snupkg`) and deterministic build settings.

---

## Usage Notes

1. For every change, prefer small bullet points written in the imperative mood (e.g. “Add …”, “Fix …”).
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

[Unreleased]: https://github.com/vmelamed/vm2.Ulid/compare/v0.1.0...HEAD
[v0.1.0]: https://github.com/vmelamed/vm2.Ulid/releases/tag/v0.1.0