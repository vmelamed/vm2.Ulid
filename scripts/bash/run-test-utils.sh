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

    $0 [<test-project-path> |
       --<long option> <value>|-<short option> <value> |
       --<long switch>|-<short switch> ]*

    The script runs the tests in the specified test project and collects code
    coverage information. It assumes that the scripts directory is located in
    the root directory of the solution.

Parameters:
    <test-project-path>     The path to the test project file. Optional.
                            Default: ${test_project}

Switches:
    --debugger              The script is running under a debugger, e.g. 'lldb'
                            or 'gdb'. If specified, the script will not set traps
                            for DEBUG and EXIT, so that the debugger can be used
                            to step through the script. It will also set the
                            '--quiet' option to true. These need to be handled
                            as early as possible, therefore it the option must
                            be specified, it has to be the first argument.
                            Default ${debugger}.

    --help | -h             Displays this usage text and exits.

    --trace | -x            Sets the Bash trace option 'set -x'.
                            Default ${trace_enabled}.

    --dry-run | -y          Runs the script without executing any commands but
                            shows what would have been executed. Default ${dry_run}.

    --quiet | -q            Suppresses all prompts, and assumes the default
                            answers.  Default ${quiet}.

    --verbose | -v          Enables verbose output. Default ${verbose}.

Options:
    --coverage-threshold | -t
                            Specifies the minimum acceptable code coverage
                            percentage (0-100). Default: ${coverage_threshold}

    --configuration | -c    Specifies the build configuration to use (Debug or
                            Release). Default: ${configuration}

    --artifacts | -a        Specifies the directory where to create the script's
                            output summary and report files. If not specified the
                            artifacts are created in the test results directory
                            from the environment variable TEST_RESULTS_DIR.
                            If the environment variable is not set, it is set to
                            a 'TestResults' subdirectory in the solution
                            directory.
                            Default: ${TEST_RESULTS_DIRECTORY}

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
    if [[ "$1" == "--debugger" ]]; then
        debugger="true"
        quiet="true"
        shift
    else
        trap on_debug DEBUG
        trap on_exit EXIT
    fi
    if [[ $CI == "true" ]]; then
        quiet="true"
    fi

    local flag
    local value

    while [[ "${#}" -gt 0 ]]; do
        # get the flag and convert it to lower case
        flag=$(to_lower "${1}")
        shift
        case "$flag" in
            --help|-h ) usage; exit 0 ;;

            --trace|-x )
                if [[ ! "${CI}" || "${CI}" != "true" ]]; then
                    trace_enabled=true
                    set -x
                fi
                ;;

            --dry-run|-y ) dry_run=true ;;

            --quiet|-q ) quiet=true ;;

            --verbose|-v ) verbose=true; _output="/dev/stdout" ;;

            --coverage-threshold|-t )
                value="$1"; shift
                if ! [[ "${value}" =~ ^[0-9]+$ ]] || (( value < 0 || value > 100 )); then
                    usage "The coverage threshold must be an integer between 0 and 100. Got \"${value}\"."
                    exit 2
                fi
                coverage_threshold=$((value + 0))  # ensure it's an integer
                ;;

            --configuration|-c )
                value="${1,,}"; shift
                if ! is_in "$value" "release" "debug"; then
                    usage "The coverage threshold must be either 'Release' or 'Debug'. Got \"${value}\"."
                    exit 2
                fi
                configuration="${value^}"
                ;;

            --artifacts|-a ) value="$1"; shift; TEST_RESULTS_DIRECTORY=$(realpath -m "${value}") ;;

            *)  value="$flag"
                local p
                p="$(realpath -m "${value}")"
                if [[ -n "$test_project" && "$test_project" != "$p" ]]; then
                    usage "More than one test project specified."
                    exit 2
                elif [[ ! -f "${p}" ]]; then
                    usage "The specified test project file \"${p}\" does not exist."
                    exit 2
                else
                    test_project="${p}"
                fi
                ;;
        esac
    done
}

dump_variables()
{
    dump_vars \
        -h "Variables:" \
        test_project \
        coverage_threshold \
        configuration \
        --line \
        solution_directory \
        script_directory \
        TEST_RESULTS_DIRECTORY \
        COVERAGE_RESULTS_DIRECTORY \
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
