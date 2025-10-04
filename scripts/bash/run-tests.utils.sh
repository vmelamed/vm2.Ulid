#!/bin/bash

# shellcheck disable=SC2034 # xyz appears unused. Verify use (or export if used externally).
function get_arguments()
{
    if [[ "${#}" -eq 0 ]]; then return; fi

    # process --debugger first
    for v in "$@"; do
        if [[ "$v" == "--debugger" ]]; then
            get_common_arg "--debugger"
            break
        fi
    done
    # shellcheck disable=SC2154 # v appears unused. Verify use (or export if used externally).
    if [[ $debugger != "true" ]]; then
        trap on_debug DEBUG
        trap on_exit EXIT
    fi

    local flag
    local value
    local p

    while [[ "${#}" -gt 0 ]]; do
        # get the flag and convert it to lower case
        flag="$1"
        shift
        if get_common_arg "$flag"; then
            continue
        fi
        case "$flag" in
            --debugger     ) ;;  # already processed above
            --help|-h      ) usage; exit 0 ;;
            --artifacts|-a ) value="$1"; shift; ARTIFACTS_DIR=$(realpath -m "$value") ;;
            --define|-d    )
                value="$1"; shift
                if ! [[ "$value" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
                    usage "The specified pre-processor symbol '$value' is not valid."
                    exit 2
                fi
                if [[ ! "$defined_symbols" =~ (^|;)"$value"($|;) ]]; then
                    defined_symbols="$value $defined_symbols"   # NOTE: space-separated!
                fi
                ;;

            --min-coverage-pct|-t )
                value="$1"; shift
                if ! [[ "$value" =~ ^[0-9]+$ ]] || (( value < 0 || value > 100 )); then
                    usage "The coverage threshold must be an integer between 0 and 100. Got '$value'."
                    exit 2
                fi
                min_coverage_pct=$((value + 0))  # ensure it's an integer
                ;;

            --configuration|-c )
                value="${1,,}"; shift
                if ! is_in "$value" "release" "debug"; then
                    usage "The coverage threshold must be either 'Release' or 'Debug'. Got '$value'."
                    exit 2
                fi
                configuration="${value^}"
                ;;

            *)  value="$flag"
                if ! p=$(realpath -e "$value"); then
                    usage "The specified test project file '$value' does not exist."
                    exit 2
                elif [[ -n "$test_project" && "$test_project" != "$p" ]]; then
                    usage "More than one test project specified: '$test_project' and '$p'."
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
        ci \
        --header "other globals:" \
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
