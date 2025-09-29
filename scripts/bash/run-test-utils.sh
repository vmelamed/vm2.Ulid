function usage()
{
    set +x
    echo "
Usage:

    $0 [debugger] [<test-project-path> |
       --<long option> <value>|-<short option> <value> |
       --<long switch>|-<short switch> ]*

    The script runs the tests in the specified test project and collects code
    coverage information. It assumes that the scripts directory is located in
    the root directory of the solution.

Parameters:
    debugger                Runs the script under a bash debugger. Implies
                            --quiet. Default ${debugger}. If needed it must be
                            specified as the first argument.

    <test-project-path>     The path to the test project file. Optional.
                            Default: ${test_project}

Switches:
    --help | -h             Displays this usage text and exits.

    --debug | -d            Turns ON the development debugging mode ON, which
                            includes also traces, diagnostic messages, debugging
                            asserts, etc. Default ${debug}.

    --trace | -x            Sets the Bash trace option 'set -x'. Default ${trace}.

    --dry-run | -y          Runs the script without executing any commands but
                            shows what would have been executed. Default ${dry_run}.

    --quiet | -q            Suppresses all prompts, and assumes the default
                            answers.  Default ${quiet}.

Options:
    --coverage-threshold | -t
                            Specifies the minimum acceptable code coverage
                            percentage (0-100). Default: ${coverage_threshold}

    --configuration | -c    Specifies the build configuration to use (Debug or
                            Release). Default: ${configuration}

    --artifacts | -a        Specifies the directory where to create the script's
                            output summary and report files.
                            Default: ${TEST_RESULTS_DIRECTORY}

"
    if [[ "$1" ]]; then
        echo "$1"
    fi
    if [[ "$trace" == "true" ]]; then
        set -x
    fi
}

function get_arguments()
{
    trace "Getting input parameters..." "$@"
    if [[ "$1" == "debugger" ]]; then
        debugger="true"
        quiet="true"
        shift
    else
        trap on_debug DEBUG
        trap on_exit EXIT
    fi

    while [[ "$1" ]]; do
        to_lower ${1}
        flag="$return_lower"
        shift
        if is_in "${flag}" "--dry-run" "-y" \
                        "--debug"   "-d" \
                        "--trace"   "-x" \
                        "--help"    "-h"
        then
            value=$1
            shift
            if [[ "${value}" =~ \.csproj ]]; then
                if [[ ! -f "${value}" ]]; then
                    usage "The test project file \"${value}\" does not exist."
                    exit 2
                fi
                if [[ ! -z "${test_project}" ]]; then
                    usage "The test project file has already been set to \"${test_project}\". Cannot set it again to \"${value}\"."
                    exit 2
                fi
            elif [[ -z "${flag}" ]]; then
                usage "Unexpected argument \"${value}\". Should be the file name of the test project."
                exit 2
            elif [[ -z "${value}" ]]; then
                usage "Expected value after flag \"${flag}\""
                exit 2
            fi
        fi
        case $flag in
            --help|-h )
                usage
                exit 0
                ;;

            --debug|-d )
                debug="true"
                if [[ ${debugger} != true ]]; then
                    _output="/dev/stdout"   # display all on the console
                fi
                ;;

            --trace|-x )
                trace=true
                set -x
                ;;

            --dry-run|-y )
                dry_run=true
                ;;

            --quiet|-q )
                if [[ ${debugger} != true ]]; then
                    quiet=true
                fi
                ;;

            --coverage-threshold|-ct )
                if ! [[ "${value}" =~ ^[0-9]+$ ]] || (( value < 0 || value > 100 )); then
                    usage "The coverage threshold must be an integer between 0 and 100. Got \"${value}\"."
                    exit 2
                fi
                coverage_threshold=${value}
                ;;

            --configuration|-c )
                to_lower ${1}
                value="$return_lower"
                if ! is_in "release" "debug"; then
                    usage "The coverage threshold must be either 'release' or 'debug'. Got \"${value}\"."
                    exit 2
                fi
                configuration="${value}"
                ;;

            --artifacts|-a )
                TEST_RESULTS_DIRECTORY=$(realpath -e "${value}")
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
