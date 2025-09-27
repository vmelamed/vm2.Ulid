
dump_vars()
{
    echo "TEST_PROJECT:                 $TEST_PROJECT"
    echo "COVERAGE_THRESHOLD:           $COVERAGE_THRESHOLD"
    echo "CONFIGURATION:                $CONFIGURATION"

    echo "p:                            $p"
    echo "base_name:                    $base_name"

    echo "test_results_results_dir:     $test_results_results_dir"

    echo "coverage_source_dir:          $coverage_source_dir"
    echo "coverage_source_fileName:     $coverage_source_fileName"
    echo "coverage_source_path:         $coverage_source_path"

    echo "coverage_reports_dir:         $coverage_reports_dir"
    echo "coverage_reports_fileName:    $coverage_reports_fileName"
    echo "coverage_reports_path:        $coverage_reports_path"

    echo "coverage_summary_dir:         $coverage_summary_dir"
    echo "coverage_summary_fileName:    $coverage_summary_fileName"
    echo "coverage_summary_path:        $coverage_summary_path"
    echo "coverage_summary_html_dir:    $coverage_summary_html_dir"

echo "test command line:"
echo "----------------------------------------"
echo "dotnet test ${TEST_PROJECT} \
    --configuration ${CONFIGURATION} -- \
    --results-directory ${test_results_results_dir} \
    --coverage \
    --coverage-output-format cobertura \
    --coverage-output ${coverage_source_path}"
}

p="$(pwd)"

export COVERAGE_THRESHOLD=80
export TEST_PROJECT="${p}/test/UlidType.Tests/UlidType.Tests.csproj"
export CONFIGURATION="Release"

export TEST_RESULTS_DIR="${p}/TestResults"
export COVERAGE_RESULTS_DIR="${p}/CoverageResults"

base_name=$(basename "${TEST_PROJECT%.*}")                                      # the base name of the test project without the file extension

test_results_results_dir="${TEST_RESULTS_DIR}/Results"                          # the directory for the log files from the test run

coverage_source_dir="${COVERAGE_RESULTS_DIR}/coverage"                          # the directory for the raw coverage files
coverage_source_fileName="coverage.cobertura.xml"                               # the name of the raw coverage file
coverage_source_path="${coverage_source_dir}/${coverage_source_fileName}"       # the path to the raw coverage file

coverage_reports_dir="${COVERAGE_RESULTS_DIR}/coverage_reports"                 # the directory for the coverage reports
coverage_reports_fileName="Summary.txt"                                         # the name of the coverage summary file
coverage_reports_path="${coverage_reports_dir}/${coverage_reports_fileName}"    # the path to the coverage summary file

coverage_summary_dir="${TEST_RESULTS_DIR}/coverage/text"                        # the directory for the text coverage summary artifacts
coverage_summary_fileName="${base_name}-TextSummary.txt"                        # the name of the coverage summary artifact file
coverage_summary_path="${coverage_summary_dir}/${coverage_summary_fileName}"    # the path to the coverage summary artifact file

coverage_summary_html_dir="${TEST_RESULTS_DIR}/coverage/html"                   # the directory for the coverage html artifacts

mkdir -p "${test_results_results_dir}"
mkdir -p "${coverage_source_dir}"
mkdir -p "${coverage_reports_dir}"
mkdir -p "${coverage_summary_dir}"

# display_all_vars
# exit 0

dotnet test "${TEST_PROJECT}" \
    --configuration "${CONFIGURATION}" -- \
    --results-directory "${test_results_results_dir}" \
    --coverage \
    --coverage-output-format cobertura \
    --coverage-output "${coverage_source_path}"

f=$(find "$(pwd)" -type f -path "*/${coverage_source_fileName}" -print -quit || true)
if [ -z "${f}" ]; then
    echo "Coverage file not found."
    exit 2
fi

if [ ! -s "${coverage_source_path}" ]; then
    echo "Coverage file is empty."
    exit 2
fi

dotnet tool list dotnet-reportgenerator-globaltool --tool-path ./tools > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "Installing reportgenerator tool..."
    mkdir -p ./tools
    dotnet tool install dotnet-reportgenerator-globaltool --tool-path ./tools --version 5.*
else
    echo "reportgenerator tool already installed."
fi

./tools/reportgenerator \
    -reports:${coverage_source_path} \
    -targetdir:"${coverage_reports_dir}" \
    -reporttypes:TextSummary,html

if [ ! -s "${coverage_reports_path}" ]; then
    echo "Coverage summary not found."
    exit 2
fi

# Copy the coverage report summary to the artifact directory
mv "${coverage_reports_path}" "${coverage_summary_path}"
mv "${coverage_reports_dir}"  "${coverage_summary_html_dir}"

# Extract the coverage percentage from the summary file
pct=$(sed -nE 's/Method coverage: ([0-9]+)(\.[0-9]+)?%.*/\1/p' "${coverage_summary_path}" | head -n1)
if [ -z "${pct}" ]; then
    echo "Could not parse coverage percent from $${coverage_summary_path}"
    exit 2
fi

echo "Coverage: ${pct}% (threshold: ${COVERAGE_THRESHOLD}%)"

# Compare the coverage percentage against the threshold
if (( pct < COVERAGE_THRESHOLD )); then
    echo "Coverage ${pct}% is below threshold ${COVERAGE_THRESHOLD}%"
    exit 2
else
    echo "Coverage ${pct}% meets threshold ${COVERAGE_THRESHOLD}%"
fi
