#!/bin/bash
set -euo pipefail

# get the default values from environment variables etc.
bash_source=${BASH_SOURCE[0]}
declare -r bash_source

script_dir="$(dirname "$(realpath -e "$bash_source")")"
declare -r script_dir

solution_dir="$(dirname "$(realpath -e "$script_dir/..")")"
declare -r solution_dir

declare bm_project=${BM_PROJECT:="$solution_dir/benchmarks/UlidType.Benchmarks/UlidType.Benchmarks.csproj"}
bm_project=$(realpath -e "$bm_project")  # ensure it's an absolute path and exists

declare -x ARTIFACTS_DIR=${ARTIFACTS_DIR:="$solution_dir/BmResults"}
ARTIFACTS_DIR=$(realpath -m "$ARTIFACTS_DIR")  # ensure it's an absolute path

declare configuration=${CONFIGURATION:="Release"}

source "$script_dir/_common.sh"
source "$script_dir/run-benchmarks-utils.sh"

get_arguments "$@"
declare -r bm_project
declare -r configuration

declare -x results_dir=${results_dir:="$ARTIFACTS_DIR/results"}
results_dir=$(realpath -m "$results_dir")  # ensure it's an absolute path
declare -r results_dir

declare -x summaries_dir=${summaries_dir:="$ARTIFACTS_DIR/summaries"}
summaries_dir=$(realpath -m "$summaries_dir")
declare -r summaries_dir

declare -x baseline_dir=${baseline_dir:="$ARTIFACTS_DIR/baseline"}
baseline_dir=$(realpath -m "$baseline_dir")
declare -r baseline_dir

dump_all_variables

trace "Creating directory(s)..."
execute mkdir -p "$summaries_dir"

trace "Running benchmark tests in project '$bm_project' with configuration '$configuration'..."
execute mkdir -p "${ARTIFACTS_DIR}"
execute dotnet run \
    --project "$bm_project" \
    --configuration "$configuration" \
    --filter '*' \
    --memory \
    --exporters JSON \
    --artifacts "$ARTIFACTS_DIR"

if ! command -v jq >/dev/null 2>&1; then
    execute sudo apt-get update && sudo apt-get install -y jq
    echo "jq successfully installed."
fi

if [[ $dry_run != "true" ]]; then

    declare -a files

    # if a glob pattern does not match any files,
    # it expands to an empty string instead of the default to leaving the pattern unchanged,
    # i.e. ${ARTIFACTS_DIR}/results/*-report.json - we don't want that
    shopt -s nullglob
        files=("$ARTIFACTS_DIR/results/*-report.json")
    shopt -u nullglob

    if [ ${#files[@]} -eq 0 ]; then
        echo "No JSON reports found." >&2
        exit 2
    fi

    for f in "${files[@]}"; do
        sf=$(sed -nE 's/(.*)-report.json/\1-summary.json/p' <<< "$(basename "${f}")")
        jq -f "$solution_dir/.github/workflows/summary.jq" "${f}" > "${summaries_dir}/${sf}"
    done
fi

#-------------------------------------------------------------------------------

fs=$(ls "${summaries_dir}"/*-summary.json 2>/dev/null || true)
if [ -z "${fs}" ]; then
    echo "No current benchmark result JSON files found." >&2
    exit 2
fi
sum_cur=0
for f in ${fs}; do
    VAL=$(jq '( .Totals.Mean // 0)' "${f}")
    sum_cur=$(( sum_cur + VAL ))
done
if (( sum_cur == 0 )); then
    echo "Current sum is invalid (${sum_cur})." >&2
    exit 2
fi

fs=$(ls "${baseline_dir}"/*-summary.json 2>/dev/null || true)
if [[ -z "${fs}" ]]; then
    echo "Baseline reports were not found at ${baseline_dir}." >&2
    echo "FORCE_NEW_BASELINE=true" >> "$GITHUB_ENV"
    exit 0
fi
sum_base=0
for f in ${fs}; do
    VAL=$(jq '( .Totals.Mean // 0)' "${f}")
    sum_base=$(( sum_base + VAL ))
done
if (( sum_base == 0 )); then
    echo "Baseline sum is invalid (${sum_base})." >&2
    exit 2
fi

pct=$(( (sum_cur - sum_base) * 100 / sum_base ))
echo "Percent change vs baseline: ${pct}% (allowed: ${max_regression_pct}%)"
flush_stdout

if (( pct > max_regression_pct )); then
    echo "Performance regression exceeds threshold" >&2
    exit 2
elif (( pct > 0 )); then
    echo "Performance regression within acceptable threshold."
    flush_stdout
elif (( pct < 0 )); then
    pct_abs=$(( -pct ))
    if (( pct_abs >= max_regression_pct )); then
        echo "Significant improvement of ${pct_abs}% over baseline. Updating the baseline."
        echo "FORCE_NEW_BASELINE=true" >> "$GITHUB_ENV"
    else
        echo "Improvement of ${pct_abs}% over baseline."
    fi
    flush_stdout
fi
