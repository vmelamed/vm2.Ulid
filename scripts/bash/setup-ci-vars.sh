#!/bin/bash
set -euo pipefail

declare -xr this_script=${BASH_SOURCE[0]}

script_name="$(basename "${this_script%.*}")"
declare -xr script_name

script_dir="$(dirname "$(realpath -e "$this_script")")"
declare -xr script_dir

source "$script_dir/_common.sh"

# CI Variables that will be passed as environment variables
declare -x matrix_os=${MATRIX_OS:-}
declare -x dotnet_version=${DOTNET_VERSION:-}
declare -x configuration=${CONFIGURATION:-}
declare -x defined_symbols=${DEFINED_SYMBOLS:-}
declare -x test_project=${TEST_PROJECT:-}
declare -x min_coverage_pct=${MIN_COVERAGE_PCT:-}
declare -x run_benchmarks=${RUN_BENCHMARKS:-}
declare -x benchmark_project=${BENCHMARK_PROJECT:-}
declare -x force_new_baseline=${FORCE_NEW_BASELINE:-}
declare -x max_regression_pct=${MAX_REGRESSION_PCT:-}
declare -x verbose=${VERBOSE:-}

source "$script_dir/setup-ci-vars.usage.sh"
source "$script_dir/setup-ci-vars.utils.sh"

get_arguments "$@"

# Validate and set matrix-os
if ! echo "$matrix_os" | jq . >/dev/null 2>&1; then
    echo "ERROR: Invalid JSON for matrix-os: $matrix_os" >&2
    exit 1
fi

# Validate and set dotnet-version
if [[ -z "$dotnet_version" ]]; then
    echo "ERROR: dotnet-version cannot be empty" >&2
    exit 1
fi

# Set configuration with validation
if [[ "$configuration" != "Release" && "$configuration" != "Debug" ]]; then
    echo "ERROR: configuration must be 'Release' or 'Debug', got: $configuration" >&2
    exit 1
fi

# Set other variables with basic validation

if [[ -z "$test_project" ]]; then
    echo "ERROR: test-project cannot be empty" >&2
    exit 1
fi

# Validate numeric inputs
if ! [[ "$min_coverage_pct" =~ ^[0-9]+$ ]] || (( min_coverage_pct < 0 || min_coverage_pct > 100 )); then
    echo "ERROR: min-coverage-pct must be 0-100, got: $min_coverage_pct" >&2
    exit 1
fi

# Boolean validations
if [[ "$run_benchmarks" != "true" && "$run_benchmarks" != "false" ]]; then
    echo "ERROR: run-benchmarks must be true/false, got: $run_benchmarks" >&2
    exit 1
fi

if [[ -z "$benchmark_project" ]]; then
    echo "ERROR: benchmark-project cannot be empty" >&2
    exit 1
fi

if [[ "$force_new_baseline" != "true" && "$force_new_baseline" != "false" ]]; then
    echo "ERROR: force-new-baseline must be true/false, got: $force_new_baseline" >&2
    exit 1
fi

if ! [[ "$max_regression_pct" =~ ^[0-9]+$ ]] || (( max_regression_pct < 0 || max_regression_pct > 100 )); then
    echo "ERROR: max-regression-pct must be 0-100, got: $max_regression_pct" >&2
    exit 1
fi

if [[ "$verbose" != "true" && "$verbose" != "false" ]]; then
    echo "ERROR: verbose must be true/false, got: $verbose" >&2
    exit 1
fi

# shellcheck disable=SC2154

{
    echo "matrix-os=$matrix_os"
    echo "dotnet-version=$dotnet_version"
    echo "configuration=$configuration"
    echo "defined-symbols=$defined_symbols"
    echo "test-project=$test_project"
    echo "min-coverage-pct=$min_coverage_pct"
    echo "run-benchmarks=$run_benchmarks"
    echo "benchmark-project=$benchmark_project"
    echo "force-new-baseline=$force_new_baseline"
    echo "max-regression-pct=$max_regression_pct"
    echo "verbose=$verbose"
} >> "$GITHUB_OUTPUT"

# Log all computed values for debugging
echo "✅ All variables validated successfully:"
echo "  matrix-os: $matrix_os"
echo "  dotnet-version: $dotnet_version"
echo "  configuration: $configuration"
echo "  defined-symbols: $defined_symbols"
echo "  test-project: $test_project"
echo "  min-coverage-pct: $min_coverage_pct"
echo "  run-benchmarks: $run_benchmarks"
echo "  benchmark-project: $benchmark_project"
echo "  force-new-baseline: $force_new_baseline"
echo "  max-regression-pct: $max_regression_pct"
echo "  verbose: $verbose"
