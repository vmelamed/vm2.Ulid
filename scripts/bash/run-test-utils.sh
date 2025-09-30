#!/bin/bash

# Lightweight bash utilities for running tests and collecting code coverage.
# Intended to be sourced by scripts/bash/run-tests.sh.
# No external deps required.
# Usage:
#   source ./scripts/bash/run-test-utils.sh
# Exits non-zero on first failure.

function usage()
{
    set +x
    echo "
Usage:

    $0 [<test-project-path>] |
       [--<long option> <value>|-<short option> <value> |
        --<long switch>|-<short switch> ]*

    This script runs the tests in the specified test project and collects code
    coverage information. It assumes that the directory 'scripts/bash' is located
    in the root directory of the solution (here: $solution_dir).

Parameters:
    <test-project-path>     The path to the test project file. Optional.
                            Initial value from \$TEST_PROJECT or
                            $test_project

Switches:
    --debugger              Set when the script is running under a debugger, e.g.
                            'gdb'. If specified, the script will not set traps
                            for DEBUG and EXIT, and will set the '--quiet'
                            switch. If needed, this option must be specified as
                            the first argument.
                            Initial value from \$DEBUGGER or 'false'

    --help | -h | -?        Displays this usage text and exits.

    --dry-run | -y          Runs the script without executing any commands but
                            shows what would have been executed.
                            Initial value from \$DRY_RUN or 'false'

    --quiet | -q            Suppresses all prompts for input from the user, and
                            assumes the default answers.
                            Initial value from \$QUIET or 'false'

    --verbose | -v          Enables verbose output all output from the invoked
                            commands (e.g. dotnet, reportgenerator) to be sent
                            to 'stdout' instead of '/dev/null'. I also enables
                            the output from the script function trace().
                            Initial value from \$VERBOSE or 'false'

    --trace | -x            Sets the Bash trace option 'set -x' and enables the
                            output from the functions 'trace' and 'dump_vars'.
                            Initial value from \$TRACE_ENABLED or 'false'

Options:
    --min_coverage_pct | -t
                            Specifies the minimum acceptable code coverage
                            percentage (0-100).
                            Initial value from \$MIN_COVERAGE_PCT or 80

    --configuration | -c    Specifies the build configuration to use ('Debug' or
                            'Release').
                            Initial value from \$CONFIGURATION or 'Release'

    --artifacts | -a        Specifies the directory where to create the script's
                            artifacts: summary, report files, etc.
                            Initial value from \$ARTIFACTS_DIR or
                            '\$solution_dir/TestResults'
                            ($solution_dir/TestResults)

"
    if [[ "${#}" -gt 0 && "$1" ]]; then
        echo "$1" >&2
    fi
    if [[ "$trace_enabled" == "true" ]]; then
        set -x
    fi
}

function get_arguments()
{
    if [[ "${#}" -eq 0 ]]; then
        return
    fi
    if [[ "$1" == "--debugger" || $debugger == "true" ]]; then
        debugger="true"
        quiet="true"
        shift
    else
        trap on_debug DEBUG
        trap on_exit EXIT
    fi

    local flag
    local value

    while [[ "${#}" -gt 0 ]]; do
        # get the flag and convert it to lower case
        flag=$(to_lower "$1")
        shift
        case "$flag" in
            --help|-h|'-?' ) usage; exit 0 ;;

            --dry-run|-y ) dry_run=true ;;

            --quiet|-q ) quiet=true ;;

            --verbose|-v ) verbose=true; trace_enabled=true; _output="/dev/stdout" ;;

            --trace|-x ) trace_enabled=true; set -x; ;;

            --artifacts|-a ) value="$1"; shift; ARTIFACTS_DIR=$(realpath -m "$value") ;;

            --min_coverage_pct|-t )
                value="$1"; shift
                if ! [[ "$value" =~ ^[0-9]+$ ]] || (( value < 0 || value > 100 )); then
                    usage "The coverage threshold must be an integer between 0 and 100. Got \"$value\"."
                    exit 2
                fi
                min_coverage_pct=$((value + 0))  # ensure it's an integer
                ;;

            --configuration|-c )
                value="${1,,}"; shift
                if ! is_in "$value" "release" "debug"; then
                    usage "The coverage threshold must be either 'Release' or 'Debug'. Got \"$value\"."
                    exit 2
                fi
                configuration="${value^}"
                ;;

            *)  value="$flag"
                local p
                p="$(realpath -m "$value")"
                if [[ -n "$test_project" && "$test_project" != "$p" ]]; then
                    usage "More than one test project specified."
                    exit 2
                elif [[ ! -f "$p" ]]; then
                    usage "The specified test project file \"$p\" does not exist."
                    exit 2
                else
                    test_project="$p"
                fi
                ;;
        esac
    done
}

dump_all_variables()
{
    dump_vars \
        --header "Script Arguments:" \
        test_project \
        debugger \
        dry_run \
        verbose \
        quiet \
        trace_enabled \
        configuration \
        min_coverage_pct \
        ARTIFACTS_DIR \
        --header other globals: \
        solution_dir \
        script_dir \
        COVERAGE_RESULTS_DIR \
        --line \
        base_name \
        --blank \
        test_results_results_dir \
        --blank \
        coverage_source_dir \
        coverage_source_fileName \
        coverage_source_path \
        --blank \
        coverage_reports_dir \
        coverage_reports_fileName \
        coverage_reports_path \
        --blank \
        coverage_summary_dir \
        coverage_summary_fileName \
        coverage_summary_path \
        coverage_summary_html_dir
}
