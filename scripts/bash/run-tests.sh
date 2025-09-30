#!/bin/bash
set -euo pipefail

declare script_dir
script_dir=$(realpath -e "$(dirname "${BASH_SOURCE[0]}")")
declare -r script_dir

declare solution_dir
solution_dir=$(realpath -e "$(dirname "$script_dir/../../.")")
declare -r solution_dir

source "$script_dir/_common.sh"

declare test_project=${TEST_PROJECT:="$solution_dir/test/UlidType.Tests/UlidType.Tests.csproj"}
declare configuration=${CONFIGURATION:="Release"}
declare -i min_coverage_pct=${MIN_COVERAGE_PCT:-80}
declare -x ARTIFACTS_DIR=${ARTIFACTS_DIR:="$solution_dir/TestResults"}
declare -x COVERAGE_RESULTS_DIR="$ARTIFACTS_DIR/CoverageResults"

source "$script_dir/run-test-utils.sh"

get_arguments "$@"

renamed_results_dir="$ARTIFACTS_DIR-$(date -u +"%Y%m%dT%H%M%S")"
declare -r renamed_results_dir

if [[ -d "$ARTIFACTS_DIR" && "$(ls -A "$ARTIFACTS_DIR")" ]]; then
    choice=$(choose \
                "The test results directory \"$ARTIFACTS_DIR\" already exists. What do you want to do?" \
                "Delete the directory and continue" \
                "Rename the directory to \"$renamed_results_dir\" and continue" \
                "Exit the script") || exit $?

    trace "User selected option: $choice"
    case $choice in
        1)  echo "Deleting the directory \"$ARTIFACTS_DIR\"..." >&2
            execute rm -rf "$ARTIFACTS_DIR" ;;
        2)  execute mv "$ARTIFACTS_DIR" "$renamed_results_dir"
            ;;
        3)  echo "Exiting the script."; exit 0 ;;
        *)  echo "Invalid option. Exiting." >&2; exit 2 ;;
    esac
fi

COVERAGE_RESULTS_DIR="$ARTIFACTS_DIR/CoverageResults"                           # the directory for the coverage results. We do
                                                                                # it here again in case the user changed the test
                                                                                # results directory.

test_results_results_dir="$ARTIFACTS_DIR/Results"                               # the directory for the log files from the test
                                                                                # run

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

# display_all_vars
# exit 0

trace "Running tests in project \"$test_project\" with configuration \"$configuration\"..."
execute dotnet test "$test_project" \
    --configuration "$configuration" -- \
    --results-directory "$test_results_results_dir" \
    --coverage \
    --coverage-output-format cobertura \
    --coverage-output "$coverage_source_path"

if [[ ! "$dry_run" ]]; then
    f=$(find "$(pwd)" -type f -path "*/$coverage_source_fileName" -print -quit || true)
    if [ -z "$f" ]; then
        echo "Coverage file not found." >&2
        exit 2
    fi

    if [ ! -s "$coverage_source_path" ]; then
        echo "Coverage file is empty." >&2
        exit 2
    fi
fi

trace "Generating coverage reports..."
uninstall_reportgenerator=false
if ! dotnet tool list dotnet-reportgenerator-globaltool --tool-path ./tools > _output 2>&1; then
    echo "Installing the tool 'reportgenerator'..." >&2
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
    echo "Uninstalling the tool 'reportgenerator'..." >&2
    execute dotnet tool uninstall dotnet-reportgenerator-globaltool --tool-path ./tools
    execute rm -rf ./tools
fi

if [[ ! "$dry_run" ]]; then
    if [ ! -s "$coverage_reports_path" ]; then
        echo "Coverage summary not found." >&2
        exit 2
    fi
fi

# Copy the coverage report summary to the artifact directory
trace "Copying coverage summary to \"$coverage_summary_path\"..."
execute mv "$coverage_reports_path" "$coverage_summary_path"
execute mv "$coverage_reports_dir"  "$coverage_summary_html_dir"

# Extract the coverage percentage from the summary file
trace "Extracting coverage percentage from \"$coverage_summary_path\"..."
if [[ ! "$dry_run" ]]; then
    pct=$(sed -nE 's/Method coverage: ([0-9]+)(\.[0-9]+)?%.*/\1/p' "$coverage_summary_path" | head -n1)
    if [ -z "$pct" ]; then
        echo "Could not parse coverage percent from $coverage_summary_path" >&2
        exit 2
    fi

    echo "Coverage: $pct% (threshold: $min_coverage_pct%)" >&2

    # Compare the coverage percentage against the threshold
    if (( pct < min_coverage_pct )); then
        echo "Coverage $pct% is below threshold $min_coverage_pct%" >&2
        exit 2
    else
        echo "Coverage $pct% meets threshold $min_coverage_pct%" >&2
    fi
fi
