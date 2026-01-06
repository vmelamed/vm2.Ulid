# Contributing to vm.Ulid

Thank you for your interest in contributing. This document explains the preferred workflow, coding standards and how to run/build/test the project locally.

## Code of conduct

Please be respectful in issues, PRs and reviews.

## How to contribute

- Open an issue to discuss larger changes or breaking features before implementing.
- For bug fixes, documentation improvements or small features:

  1. Fork the repo and create a topic branch from `main`:
     - Branch name format: `fix/...`, `feat/...`, `docs/...`, `test/...`.
  2. Make focused commits with clear messages.
  3. Open a pull request against `main` referencing the issue (if any).

Commit message guidance:

- Use short subject line (max ~72 chars) and an optional body.
- Include `Fixes #NN` when closing an issue.

## Development setup

Prerequisites:100:

- .NET 10 SDK installed (download from Microsoft).
- A modern editor or IDE, for example __Visual Studio Code__ (preferred) or  __Visual Studio__, updated to support .NET 10.
- Optional: `dotnet-format` for automatic formatting.

Clone and build locally

Run tests

Recommended IDE actions

- In Visual Studio open the solution and use __Build Solution__ or __Rebuild__.
- Use an EditorConfig file (repo may include one) to enforce formatting rules.

Formatting and linting

- Run `dotnet format` to apply consistent formatting.
- Prefer small, readable changes and run tests before submitting a PR.

## API and documentation

- Update XML docs and `src/UlidType/README.md` for any public API changes.
- Add or update examples in `examples/` as needed.

## Tests

- In Visual Studio Code and CL the code must be tested using `dotnet run` - MTP v2 and in Visual Studio use the built-in test explorer - MTP v1. For settings and packages differences see Directory.Builds.props.
- Add unit tests for bug fixes and new features under `test/UlidType.Tests`.
- Tests should be deterministic where possible and runnable with `dotnet test`.
- The code coverage should not go under 80%

##  Benchmarks

- Benchmarks are located in the `benchmarks/` folder.
- Use `dotnet run -c Release` to execute benchmarks.
- Add benchmarks for new features or performance improvements. The goal is to track the performance with each change and to not go over 20% threshold. (If a performance regression is detected, investigate and optimize the code before merging or justify in a discussion.)

## Pull request checklist

- [ ] PR targets `main`
- [ ] Branch name follows convention
- [ ] Unit tests added/updated and passing
- [ ] Benchmarks added/updated if applicable and performance did not regress beyond the acceptable 20% threshold
- [ ] Documentation updated if behavior or public API changed
- [ ] CI checks pass (build & tests)
- [ ] Include description of the change and motivation

## Continuous Integration / Releases

- The repository uses GitHub Actions for CI. CI should build the project and run tests on PRs.
- Releases should follow semantic versioning.

## Licensing & Intellectual Property

- Verify that your contribution can be licensed under the repository license (MIT).
- Do not include third-party code without proper license and attribution.
- Make sure to prepend your source files with the appropriate SPDX license identifier header.

## Questions

If you are unsure about design choices or need guidance, open an issue and label it `discussion` or `help wanted`.

Thank you for contributing!
