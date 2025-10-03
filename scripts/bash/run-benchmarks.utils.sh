#!/bin/bash

# shellcheck disable=SC2034 # xyz appears unused. Verify use (or export if used externally).
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
    local p

    while [[ "${#}" -gt 0 ]]; do
        # get the flag and convert it to lower case
        flag="$1"; shift
        if get_common_arg "$flag"; then
            continue
        fi
        case "$flag" in
            --help|-h ) usage; exit 0 ;;

            --configuration|-c )
                value="${1,,}"; shift
                if ! is_in "$value" "release" "debug"; then
                    usage "The coverage threshold must be either 'Release' or 'Debug'. Got '$value'."
                    exit 2
                fi
                configuration="${value^}"
                ;;

            --define|-d )
                value="$1"; shift
                if ! [[ "$value" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
                    usage "The specified pre-processor symbol '$value' is not valid."
                    exit 2
                fi
                if [[ -z "$define" ]]; then
                    define="$value"
                elif [[ ! "$define" =~ (^|;)"$value"($|;) ]]; then
                    define="$define;$value"
                fi
                ;;

            --force-new-baseline|-f ) force_new_baseline=true ;;

            --artifacts|-a ) value="$1"; shift; ARTIFACTS_DIR=$(realpath -m "$value") ;;

            --max-regression-pct|-r )
                value="$1"; shift
                if ! [[ "$value" =~ ^[0-9]+$ ]] || (( value < 0 || value > 100 )); then
                    usage "$(usage_text)" "The regression threshold must be an integer between 0 and 100. Got '$value'."
                    exit 2
                fi
                max_regression_pct=$((value + 0))  # ensure it's an integer
                ;;

            --short-run|-s )
                if [[ -z "$define" ]]; then
                    define="SHORT_RUN"
                elif [[ ! "$define" =~ (^|;)SHORT_RUN($|;) ]]; then
                    define="$define;SHORT_RUN"
                fi
                ;;

            *)  value="$flag"
                dump_vars value
                if ! p=$(realpath -e "$value"); then
                    usage "The specified test project file $value does not exist."
                    exit 2
                elif [[ -n "$bm_project" && "$bm_project" != "$p" ]]; then
                    usage "More than one test project specified: $bm_project and $p."
                    exit 2
                else
                    bm_project="$p"
                fi
                ;;
        esac
    done
}

dump_all_variables()
{
    dump_vars \
        --header "Script Arguments:" \
        bm_project \
        debugger \
        dry_run \
        verbose \
        quiet \
        trace_enabled \
        configuration \
        DEFINE \
        max_regression_pct \
        ARTIFACTS_DIR \
        --header "other globals:" \
        solution_dir \
        script_dir \
        --line \
        results_dir \
        summaries_dir \
        baseline_dir
}
