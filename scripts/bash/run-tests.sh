#!/bin/bash
set -e -o pipefail

declare -r script_directory=$(realpath -e $(dirname "${BASH_SOURCE[0]}"))

source "${script_directory}/_common.sh"

declare solution_directory=$(realpath -e $(dirname "${script_directory}/../../.."))
declare test_project="${solution_directory}/test/UlidType.Tests/UlidType.Tests.csproj"
declare configuration="Release"
declare -i coverage_threshold=80
declare -x TEST_RESULTS_DIRECTORY="${solution_directory}/TestResults"
declare -x COVERAGE_RESULTS_DIRECTORY="${TEST_RESULTS_DIRECTORY}/CoverageResults"

source "${script_directory}/run-test-utils.sh"

debug=true
echo $@
trace "trying to trace"

get_arguments $@
dump_vars \
    script_directory \
    solution_directory \
    test_project \
    configuration \
    coverage_threshold \
    TEST_RESULTS_DIRECTORY \
    COVERAGE_RESULTS_DIRECTORY


if [[ -d "${TEST_RESULTS_DIRECTORY}" && "$(ls -A "${TEST_RESULTS_DIRECTORY}")" ]]; then
    choose \
        "The test results directory \"${TEST_RESULTS_DIRECTORY}\" already exists and is not empty.
What do you want to do? Options:" \
        "Delete the directory and continue" \
        "Rename the directory to \"${TEST_RESULTS_DIRECTORY}_<UTC timestamp>\" and continue" \
        "Exit the script"

    trace "User selected option: ${choice_option}"
    case $choice_option in
        1)  echo "Deleting the directory \"${TEST_RESULTS_DIRECTORY}\"..."
            execute rm -rf "${TEST_RESULTS_DIRECTORY}"
            execute mkdir -p "${TEST_RESULTS_DIRECTORY}"
            ;;
        2)  execute mv "${TEST_RESULTS_DIRECTORY}" "${TEST_RESULTS_DIRECTORY}_$(date -u +"%Y%m%dT%H%M%S")"
            execute mkdir -p "${TEST_RESULTS_DIRECTORY}"
            ;;
        3)  echo "Exiting the script."
            exit 0
            ;;
        *)  echo "Invalid option. Exiting.";
            exit 2
            ;;
    esac
fi

COVERAGE_RESULTS_DIRECTORY="${TEST_RESULTS_DIRECTORY}/CoverageResults"          # the directory for the coverage results. We do
                                                                                # it here again in case the user changed the test
                                                                                # results directory.

base_name=$(basename "${test_project%.*}")                                      # the base name of the test project without the
                                                                                # file extension

test_results_results_dir="${TEST_RESULTS_DIRECTORY}/Results"                    # the directory for the log files from the test
                                                                                # run

coverage_source_dir="${COVERAGE_RESULTS_DIRECTORY}/coverage"                    # the directory for the raw coverage files
coverage_source_fileName="coverage.cobertura.xml"                               # the name of the raw coverage file
coverage_source_path="${coverage_source_dir}/${coverage_source_fileName}"       # the path to the raw coverage file

coverage_reports_dir="${COVERAGE_RESULTS_DIRECTORY}/coverage_reports"           # the directory for the coverage reports
coverage_reports_fileName="Summary.txt"                                         # the name of the coverage summary file
coverage_reports_path="${coverage_reports_dir}/${coverage_reports_fileName}"    # the path to the coverage summary file

coverage_summary_dir="${TEST_RESULTS_DIRECTORY}/coverage/text"                  # the directory for the text coverage summary
                                                                                # artifacts
coverage_summary_fileName="${base_name}-TextSummary.txt"                        # the name of the coverage summary artifact file
coverage_summary_path="${coverage_summary_dir}/${coverage_summary_fileName}"    # the path to the coverage summary artifact file

coverage_summary_html_dir="${TEST_RESULTS_DIRECTORY}/coverage/html"             # the directory for the coverage html artifacts

if ((debug)); then
    dump_variables
fi

trace "Creating directories..."
execute mkdir -p "${test_results_results_dir}"
execute mkdir -p "${coverage_source_dir}"
execute mkdir -p "${coverage_reports_dir}"
execute mkdir -p "${coverage_summary_dir}"

# display_all_vars
# exit 0

trace "Running tests in project \"${test_project}\" with configuration \"${configuration}\"..."
execute dotnet test "${test_project}" \
    --configuration "${configuration}" -- \
    --results-directory "${test_results_results_dir}" \
    --coverage \
    --coverage-output-format cobertura \
    --coverage-output "${coverage_source_path}"

if [[ ! "${dry_run}" ]]; then
    f=$(find "$(pwd)" -type f -path "*/${coverage_source_fileName}" -print -quit || true)
    if [ -z "${f}" ]; then
        echo "Coverage file not found."
        exit 2
    fi

    if [ ! -s "${coverage_source_path}" ]; then
        echo "Coverage file is empty."
        exit 2
    fi
fi

trace "Generating coverage reports..."
reportgenerator_installed=false
dotnet tool list dotnet-reportgenerator-globaltool --tool-path ./tools > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "Installing reportgenerator tool..."
    execute mkdir -p ./tools
    execute dotnet tool install dotnet-reportgenerator-globaltool --tool-path ./tools --version 5.*
    reportgenerator_installed=true
else
    echo "reportgenerator tool already installed."
fi
execute ./tools/reportgenerator \
    -reports:${coverage_source_path} \
    -targetdir:"${coverage_reports_dir}" \
    -reporttypes:TextSummary,html
if [[ "${reportgenerator_installed}" = "true" ]]; then
    echo "Uninstalling reportgenerator tool..."
    execute dotnet tool uninstall dotnet-reportgenerator-globaltool --tool-path ./tools
    execute rm -rf ./tools
fi

if [[ ! "${dry_run}" ]]; then
    if [ ! -s "${coverage_reports_path}" ]; then
        echo "Coverage summary not found."
        exit 2
    fi
fi

# Copy the coverage report summary to the artifact directory
trace "Copying coverage summary to \"${coverage_summary_path}\"..."
execute mv "${coverage_reports_path}" "${coverage_summary_path}"
execute mv "${coverage_reports_dir}"  "${coverage_summary_html_dir}"

# Extract the coverage percentage from the summary file
trace "Extracting coverage percentage from \"${coverage_summary_path}\"..."
if [[ ! "${dry_run}" ]]; then
    pct=$(sed -nE 's/Method coverage: ([0-9]+)(\.[0-9]+)?%.*/\1/p' "${coverage_summary_path}" | head -n1)
    if [ -z "${pct}" ]; then
        echo "Could not parse coverage percent from $${coverage_summary_path}"
        exit 2
    fi

    echo "Coverage: ${pct}% (threshold: ${coverage_threshold}%)"

    # Compare the coverage percentage against the threshold
    if (( pct < coverage_threshold )); then
        echo "Coverage ${pct}% is below threshold ${coverage_threshold}%"
        exit 2
    else
        echo "Coverage ${pct}% meets threshold ${coverage_threshold}%"
    fi
fi
