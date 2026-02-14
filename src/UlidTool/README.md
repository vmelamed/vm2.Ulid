# UlidTool - ULID Generator Command Line Tool

<!-- TOC tocDepth:2..5 chapterDepth:2..6 -->

- [Installation](#installation)
- [Quick Start](#quick-start)
- [What is a ULID?](#what-is-a-ulid)
- [Command Line Options](#command-line-options)
- [Output Formats](#output-formats)
- [Examples](#examples)
  - [Generate test data](#generate-test-data)
  - [Create GUID-compatible IDs](#create-guid-compatible-ids)
  - [Inspect ULID structure](#inspect-ulid-structure)
- [Format Abbreviation](#format-abbreviation)
- [Related Packages](#related-packages)
- [License](#license)

<!-- /TOC -->

A CLI tool for generating ULIDs (Universally Unique Lexicographically Sortable Identifiers).

## Installation

```bash
dotnet tool install -g vm2.UlidTool
```

## Quick Start

```bash
# Generate a single ULID
ulid

# Generate multiple (5) ULIDs
ulid -n 5

# Generate 3 ULIDs in GUID-like format
ulid -n 3 -f guid

# Generate a single ULID with detailed information
ulid -f detailed
```

## What is a ULID?

ULID (Universally Unique Lexicographically Sortable Identifier) is a 128-bit identifier that combines:

- **48-bit timestamp** (millisecond precision)
- **80-bit randomness**

Benefits over traditional GUIDs/UUIDs:

- ✅ **Lexicographically sortable** by creation time
- ✅ **Compact** - 26 characters vs 36 for GUIDs
- ✅ **URL-safe** - Uses Crockford's Base32 encoding
- ✅ **Monotonic** - Within the same millisecond, values increment
- ✅ **Compatible** - Can be converted to/from GUID

## Command Line Options

```text
ulid [options]

Options:
  -n, --number <number>  Number of ULIDs to generate (1-10000) [default: 1]
  -f, --format <format>  Output format [default: ulid]
  -h, --help             Show help information
  --version              Show version information
```

## Output Formats

1. ULID (default)

   26-character Base32 string, lexicographically sortable:

   ```bash
   ulid
   # 01K5ETWXTDG0ZK9PP9WMC6V4HY
   ```

1. GUID-like

   36-character hexadecimal GUID-like format:

   ```bash
   ulid -f guid
   # 01934b8e-c6d9-7f40-9a99-f1c66d8c568e
   ```

1. Detailed

   Multi-line display showing the two main string formats and the two components - timestamp and random bytes:

   ```bash
   ulid -f detailed
   # ULID: 01K5ETWXTDG0ZK9PP9WMC6V4HY
   # GUID: 01934b8e-c6d9-7f40-9a99-f1c66d8c568e
   #   Timestamp:    01K5ETWXTD      2024-01-15T10:30:45.123+00:00 (1705315845123)
   #   Random Bytes: G0ZK9PP9WMC6V4HY [ 0x7F, 0x40, 0x9A, 0x99, 0xF1, 0xC6, 0x6D, 0x8C, 0x56, 0x8E ]
   ```

### Format Abbreviation

The `--format` option accepts any unique prefix (case-insensitive):

- `u`, `ul`, `uli`, `ulid` → ULID format
- `g`, `gu`, `gui`, `guid` → GUID format
- `d`, `de`, `det`, ... `detailed` → Detailed format

## Examples

1. Generate test data

   ```bash
   # Generate 100 ULIDs for database seeding
   ulid -n 100 > test-ids.txt
   ```

1. Create GUID-compatible IDs

   ```bash
   # Generate ULIDs in GUID format for legacy systems
   ulid -n 10 -f g
   ```

1. Inspect ULID structure

   ```bash
   # Examine the components of a ULID
   ulid -f d
   ```

## Related Packages

- **[vm2.Ulid](https://www.nuget.org/packages/vm2.Ulid)** - ULID library for .NET applications
- **[ULID Specification](https://github.com/ulid/spec)** - Official ULID spec

## License

MIT - See [LICENSE](https://github.com/vmelamed/vm2.Ulid/blob/main/LICENSE)
