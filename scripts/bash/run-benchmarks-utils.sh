#!/bin/bash

function usage()
{
    set +x

    # shellcheck disable=SC2154 # solution_dir is referenced but not assigned.
    echo "
Usage:

    $0 [<bm-project-path>] |
       [--<long option> <value>|-<short option> <value> |
        --<long switch>|-<short switch> ]*

    This script runs the benchmark tests in the specified project. It assumes
    that the directory 'scripts/bash' is located in the root directory of the
    solution (here: $solution_dir).

Parameters:
    <bm-project-path>       The path to the benchmark project file. Optional.
                            Initial value from \$BM_PROJECT or
                            $bm_project

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

    --verbose | -v          Enables verbose output: all output from the invoked
                            commands (e.g. dotnet, jq, etc.) to be sent to
                            'stdout' instead of '/dev/null'. It also enables the
                            output from the script function trace().
                            Initial value from \$VERBOSE or 'false'

    --trace | -x            Sets the Bash trace option 'set -x' and enables the
                            output from the functions 'trace' and 'dump_vars'.
                            Initial value from \$TRACE_ENABLED or 'false'

    --short-run | -s        A shortcut for '--define SHORT_RUN'. See below.
                            Initial value from \$DEFINE.

    --force-new-baseline | -f
                            When specified, a new baseline will be created even
                            if a previous baseline already exists.
                            Initial value from \$FORCE_NEW_BASELINE or 'false'

Options:
    --define | -d           Defines one or more user-defined pre-processor
                            symbols to be used when building the benchmark
                            project, e.g. 'SHORT_RUN'. Which generates a shorter
                            and faster, but less accurate benchmark run. You can
                            specify this option multiple times to define multiple
                            symbols.
                            Initial value from \$DEFINE or ''

    --configuration | -c    Specifies the build configuration to use ('Debug' or
                            'Release').
                            Initial value from \$CONFIGURATION or 'Release'

    --artifacts | -a        Specifies the directory where to create the
                            benchmark artifacts: results, summaries, base lines,
                            etc.
                            Initial value from \$ARTIFACTS_DIR or
                            '\$solution_dir/BmArtifacts'
                            ($solution_dir/BmArtifacts)

    --max-regression-pct | -r
                            Specifies the maximum acceptable regression percentage
                            (0-100) when comparing to a previous, base-line
                            benchmark results.
                            Initial value from \$MAX_REGRESSION_PCT or 10

"
    if [[ "${#}" -gt 0 && -n "$1" ]]; then
        echo "$1" >&2
    fi
    if [[ "$trace_enabled" == "true" ]]; then
        set -x
    fi
    flush_stdout
}

declare -x DEFINE="${DEFINE:-}"

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
        case "$flag" in
            --help|-h ) usage; exit 0 ;;

            --dry-run|-y ) dry_run=true ;;

            --quiet|-q ) quiet=true ;;

            --verbose|-v ) verbose=true; trace_enabled=true; _ignore="/dev/stdout" ;;

            --trace|-x ) trace_enabled=true; set -x ;;

            --force-new-baseline|-f ) force_new_baseline=true ;;

            --artifacts|-a ) value="$1"; shift; ARTIFACTS_DIR=$(realpath -m "$value") ;;

            --max-regression-pct|-r )
                value="$1"; shift
                if ! [[ "$value" =~ ^[0-9]+$ ]] || (( value < 0 || value > 100 )); then
                    usage "The regression threshold must be an integer between 0 and 100. Got '$value'."
                    exit 2
                fi
                max_regression_pct=$((value + 0))  # ensure it's an integer
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

            --short-run|-s )
                if [[ -z "$define" ]]; then
                    define="SHORT_RUN"
                elif [[ ! "$define" =~ (^|;)SHORT_RUN($|;) ]]; then
                    define="$define;SHORT_RUN"
                fi
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
