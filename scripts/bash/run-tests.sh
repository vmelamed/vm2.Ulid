#!/bin/bash
set -euo pipefail

this_script=${BASH_SOURCE[0]}
declare -xr this_script

script_name="$(basename "${this_script%.*}")"
declare -xr script_name

script_dir="$(dirname "$(realpath -e "$this_script")")"
declare -xr script_dir

source "$script_dir/_common.sh"

# get the default values from environment variables etc.
solution_dir="$(dirname "$(realpath -e "$script_dir/..")")"
declare -r solution_dir

declare test_project=${TEST_PROJECT:="$solution_dir/test/UlidType.Tests/UlidType.Tests.csproj"}
test_project=$(realpath -e "$test_project")  # ensure it's an absolute path

declare -x ARTIFACTS_DIR=${ARTIFACTS_DIR:="$solution_dir/TestArtifacts"}
ARTIFACTS_DIR=$(realpath -m "$ARTIFACTS_DIR")  # ensure it's an absolute path

declare -x COVERAGE_RESULTS_DIR

declare configuration=${CONFIGURATION:="Release"}
declare -i min_coverage_pct=${MIN_COVERAGE_PCT:-80}

declare define=${DEFINE:-}

source "$script_dir/run-tests.usage.sh"
source "$script_dir/run-tests.utils.sh"

get_arguments "$@"
declare -r test_project
declare -ri min_coverage_pct
declare -r configuration
declare -r define

renamed_results_dir="$ARTIFACTS_DIR-$(date -u +"%Y%m%dT%H%M%S")"
declare -r renamed_results_dir

if [[ -d "$ARTIFACTS_DIR" && -n "$(ls -A "$ARTIFACTS_DIR")" ]]; then
    choice=$(choose \
                "The test results directory '$ARTIFACTS_DIR' already exists. What do you want to do?" \
                    "Delete the directory and continue" \
                    "Rename the directory to '$renamed_results_dir' and continue" \
                    "Exit the script") || exit $?

    trace "User selected option: $choice"
    case $choice in
        1)  echo "Deleting the directory '$ARTIFACTS_DIR'..." >&2; execute rm -rf "$ARTIFACTS_DIR" ;;
        2)  execute mv "$ARTIFACTS_DIR" "$renamed_results_dir" ;;
        3)  echo "Exiting the script."; exit 0 ;;
        *)  echo "Invalid option $choice. Exiting." >&2; exit 2 ;;
    esac
fi

COVERAGE_RESULTS_DIR="$ARTIFACTS_DIR/CoverageResults"                           # the directory for the coverage results. We do
declare -r COVERAGE_RESULTS_DIR                                                 # it here again in case the user changed the test
                                                                                # results directory.

test_results_results_dir="$ARTIFACTS_DIR/Results"                               # the directory for the log files from the test
declare -r test_results_results_dir                                             # run

coverage_source_dir="$COVERAGE_RESULTS_DIR/coverage"                            # the directory for the raw coverage files
coverage_source_fileName="coverage.cobertura.xml"                               # the name of the raw coverage file
coverage_source_path="$coverage_source_dir/$coverage_source_fileName"           # the path to the raw coverage file

coverage_reports_dir="$COVERAGE_RESULTS_DIR/coverage_reports"                   # the directory for the coverage reports
coverage_reports_fileName="Summary.txt"                                         # the name of the coverage summary file
coverage_reports_path="$coverage_reports_dir/$coverage_reports_fileName"        # the path to the coverage summary file

coverage_summary_dir="$ARTIFACTS_DIR/coverage/text"                             # the directory for the text coverage summary
                                                                                # artifacts

base_name=$(basename "${test_project%.*}")                                      # the base name of the test project without the
                                                                                # path and file extension
coverage_summary_fileName="$base_name-TextSummary.txt"                          # the name of the coverage summary artifact file
coverage_summary_path="$coverage_summary_dir/$coverage_summary_fileName"        # the path to the coverage summary artifact file
coverage_summary_html_dir="$ARTIFACTS_DIR/coverage/html"                        # the directory for the coverage html artifacts

dump_all_variables

trace "Creating directories..."
execute mkdir -p "$test_results_results_dir"
execute mkdir -p "$coverage_source_dir"
execute mkdir -p "$coverage_reports_dir"
execute mkdir -p "$coverage_summary_dir"

trace "Running tests in project '$test_project' with configuration '$configuration'..."
execute dotnet test "$test_project" \
    /p:DefineConstants="$define" \
    --configuration "$configuration" -- \
    --results-directory "$test_results_results_dir" \
    --coverage \
    --coverage-output-format cobertura \
    --coverage-output "$coverage_source_path"

if [[ $dry_run != "true" ]]; then
    f=$(find "$(pwd)" -type f -path "*/$coverage_source_fileName" -print -quit || true)
    if [[ -z "$f" ]]; then
        echo "Coverage file not found." >&2
        exit 2
    fi

    if [[ ! -s "$coverage_source_path" ]]; then
        echo "Coverage file is empty." >&2
        exit 2
    fi
fi

trace "Generating coverage reports..."
uninstall_reportgenerator=false
if ! dotnet tool list dotnet-reportgenerator-globaltool --tool-path ./tools > /dev/null; then
    echo "Installing the tool 'reportgenerator'..."; flush_stdout
    execute mkdir -p ./tools
    execute dotnet tool install dotnet-reportgenerator-globaltool --tool-path ./tools --version 5.*
    uninstall_reportgenerator=true
else
    echo "The tool 'reportgenerator' is already installed." >&2
fi
execute ./tools/reportgenerator \
    -reports:"$coverage_source_path" \
    -targetdir:"$coverage_reports_dir" \
    -reporttypes:TextSummary,html
if [[ "$uninstall_reportgenerator" = "true" ]]; then
    echo "Uninstalling the tool 'reportgenerator'..."; flush_stdout
    execute dotnet tool uninstall dotnet-reportgenerator-globaltool --tool-path ./tools
    execute rm -rf ./tools
fi

if [[ $dry_run != "true" ]]; then
    if [[ ! -s "$coverage_reports_path" ]]; then
        echo "Coverage summary not found." >&2
        exit 2
    fi
fi

# Copy the coverage report summary to the artifact directory
trace "Copying coverage summary to '$coverage_summary_path'..."
execute mv "$coverage_reports_path" "$coverage_summary_path"
execute mv "$coverage_reports_dir"  "$coverage_summary_html_dir"

# Extract the coverage percentage from the summary file
trace "Extracting coverage percentage from '$coverage_summary_path'..."
if [[ $dry_run != "true" ]]; then
    pct=$(sed -nE 's/Method coverage: ([0-9]+)(\.[0-9]+)?%.*/\1/p' "$coverage_summary_path" | head -n1)
    if [[ -z "$pct" ]]; then
        echo "Could not parse coverage percent from $coverage_summary_path" >&2
        exit 2
    fi

    echo "Coverage: $pct% (threshold: $min_coverage_pct%)"; flush_stdout

    # Compare the coverage percentage against the threshold
    if (( pct < min_coverage_pct )); then
        echo "Coverage $pct% is below threshold $min_coverage_pct%" >&2
        exit 2
    else
        echo "Coverage $pct% meets threshold $min_coverage_pct%"; flush_stdout
    fi
fi
