# Copilot Instructions

## Project Overview

This is **vm2.Ulid** - a fast, spec-compliant ULID (Universally Unique Lexicographically Sortable Identifier) implementation for .NET. The project follows a clean, single-purpose library design with comprehensive testing, benchmarking, and automated CI/CD.

### Core Architecture
- **Main Library**: `src/UlidType/` - Contains `Ulid` struct and `UlidFactory` class
- **Thread Safety**: UlidFactory ensures monotonic generation within milliseconds via internal locking
- **Performance Focus**: Optimized for speed with unsafe operations, stackalloc, and minimal allocations
- **Dual JSON Support**: Converters for both Newtonsoft.Json and System.Text.Json

## Development Patterns

### Build & Test Commands
```bash
# Standard development workflow
dotnet build                                    # Default Debug build
dotnet test                                     # Run all tests
dotnet build -c Release                         # Release build (for benchmarks)

# Benchmarks (must be Release mode)
dotnet run --project benchmarks/UlidType.Benchmarks/UlidType.Benchmarks.csproj -c Release

# Package inspection (uses MinVer for versioning)
dotnet build -c Release /p:MinVerVerbosity=detailed
```

### Script Organization
- Keep shell scripts in `scripts/bash/` directory
- Follow the established **three-file pattern** for complex scripts:
  - **Main script** (e.g., `script-name.sh`): Contains core business logic only
  - **Usage file** (e.g., `script-name.usage.sh`): Contains help text and documentation  
  - **Utils file** (e.g., `script-name.utils.sh`): Contains argument parsing and utility functions
- Use common utility functions from `_common.sh`
- Centralize common parameters in `_common.sh` via `$common_switches`

### CI/CD Patterns
- **Setup Job Pattern**: Use centralized `setup-ci-vars.sh` for DRY variable management
- **Language Separation**: Extract complex logic to dedicated script files rather than embedding in YAML
- **Environment Variable Validation**: Validate inputs at script level, not in GitHub Actions YAML
- **Skip CI Logic**: Support `[skip ci]` in commit messages for push events

### Code Quality Standards
- **Strict Warnings**: `TreatWarningsAsErrors=True` with `WarningLevel=9999`
- **Nullable Reference Types**: Enabled throughout (`<Nullable>enable</Nullable>`)
- **Unsafe Operations**: Used strategically for performance (`AllowUnsafeBlocks=true`)
- **EditorConfig**: Enforced via `EnforceCodeStyleInBuild=True`

## Testing & Benchmarking

### Test Structure
```csharp
// Use FluentAssertions for readable test assertions
ulid1.Should().BeLessThan(ulid2);
result.Should().Be(ulid);

// Test round-trips between formats
[Theory]
[MemberData(nameof(KnownGoodValues))]
public void RoundTrip_Known_GoodValues(string s) { /* ... */ }
```

### Benchmark Patterns
```csharp
// Use BenchmarkDotNet attributes consistently
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
[JsonExporter]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]

// Compare against Guid as baseline
[Benchmark(Description = "Guid.NewGuid", Baseline = true)]
[Benchmark(Description = "Ulid.NewUlid")]
```

## Release & Versioning

### Automated Versioning (MinVer)
- **Stable releases**: Manual annotated tags (`git tag -a v1.2.3 -m "v1.2.3"`)
- **Prereleases**: Auto-generated on merge to `main` (format: `v1.2.4-preview.20250107.123`)
- **Local builds**: Use computed prerelease versions when no tag reachable

### Key Commands
```bash
# Manual stable release
git tag -a v1.2.3 -m "v1.2.3" && git push origin v1.2.3

# Trigger prerelease (merge to main or empty commit)
git commit --allow-empty -m "Trigger prerelease" && git push

# Version verification
dotnet build -c Release /p:MinVerVerbosity=detailed
```

## Project-Specific Conventions

### Namespace & Assembly
- **Root Namespace**: `vm2` (not `vm2.Ulid`)
- **Package ID**: `vm2.Ulid`
- **Target Framework**: .NET 9.0 exclusively

### Performance Optimizations
- Use `stackalloc` for temporary byte arrays
- Minimize allocations in hot paths (UlidFactory.NewUlid ~40B allocation)
- Leverage `ReadOnlyMemory<byte>` for internal storage
- Prefer `TryWrite` patterns over string allocations

### JSON Integration
```csharp
// Dual converter attributes on Ulid struct
[Newtonsoft.Json.JsonConverter(typeof(NsJson.UlidNsConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(SysJson.UlidSysConverter))]
```

## Workflow Integration

### GitHub Actions Dependencies
- Secrets: `NUGET_API_KEY` for package publishing
- Matrix builds: JSON-configured OS arrays (`["ubuntu-latest"]`)
- Reusable workflows: `build.yaml`, `test.yaml`, `benchmarks.yaml`
- Skip CI: Honors `[skip ci]` in push commits only (not PRs)

### Common Issues & Solutions
- **JSON Parsing Errors**: Use `fromJSON(value || fallback || 'default')` not `fromJSON(value) || fromJSON(fallback)`
- **Verbose Validation**: Let `_common.sh` handle verbose flag; don't double-validate in setup scripts
- **YAML Folded Scalars**: Avoid indented conditionals with `>-` operator in GitHub Actions

This project emphasizes performance, reliability, and maintainable automation - follow these patterns when making changes.
