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

declare -x DEFINE="${DEFINE:-}"

declare bm_project=${BM_PROJECT:="$solution_dir/benchmarks/UlidType.Benchmarks/UlidType.Benchmarks.csproj"}
bm_project=$(realpath -e "$bm_project")  # ensure it's an absolute path and exists

declare -x ARTIFACTS_DIR=${ARTIFACTS_DIR:="$solution_dir/BmArtifacts"}
ARTIFACTS_DIR=$(realpath -m "$ARTIFACTS_DIR")  # ensure it's an absolute path

declare -x BASELINE_DIR=${BASELINE_DIR:="$ARTIFACTS_DIR/baseline"}
BASELINE_DIR=$(realpath -m "$BASELINE_DIR")  # ensure it's an absolute path

declare -x SUMMARIES_DIR=${SUMMARIES_DIR:="$ARTIFACTS_DIR/summaries"}
SUMMARIES_DIR=$(realpath -m "$SUMMARIES_DIR")  # ensure it's an absolute path

declare -x force_new_baseline=${FORCE_NEW_BASELINE:-false}

declare configuration=${CONFIGURATION:="Release"}

declare define=${DEFINE:-}

source "$script_dir/run-benchmarks.usage.sh"
source "$script_dir/run-benchmarks.utils.sh"

get_arguments "$@"
declare -r bm_project
declare -r configuration
declare -r force_new_baseline
declare -r define

declare -x results_dir=${results_dir:="$ARTIFACTS_DIR/results"}
results_dir=$(realpath -m "$results_dir")  # ensure it's an absolute path
declare -r results_dir

declare -x summaries_dir=${summaries_dir:="$SUMMARIES_DIR"}
summaries_dir=$(realpath -m "$summaries_dir")
declare -r summaries_dir

declare -x baseline_dir=${baseline_dir:="$BASELINE_DIR"}
baseline_dir=$(realpath -m "$baseline_dir")
declare -r baseline_dir

max_regression_pct=${MAX_REGRESSION_PCT:-10}
declare -ri max_regression_pct

renamed_artifacts_dir="$ARTIFACTS_DIR-$(date -u +"%Y%m%dT%H%M%S")"
declare -r renamed_artifacts_dir

dump_all_variables

if [[ -d "$ARTIFACTS_DIR" && -n "$(ls -A "$ARTIFACTS_DIR")" ]]; then
    choice=$(choose \
                "The benchmark results directory '$ARTIFACTS_DIR' already exists. What do you want to do?" \
                    "Overwrite the contents of the directory '$ARTIFACTS_DIR'" \
                    "Move the contents of the directory to '$renamed_artifacts_dir', except for the base line '$baseline_dir' (if exists), and continue" \
                    "Delete the contents of the directory, except for the base line '$baseline_dir' (if exists), and continue" \
                    "Exit the script") || exit $?

    trace "User selected option: $choice"
    case $choice in
        1)  echo "Overwriting the contents of the directory '$ARTIFACTS_DIR'..." >&2;
            ;;
        2)  echo "Moving the contents of the directory '$ARTIFACTS_DIR' to '$renamed_artifacts_dir', except for the base line '$baseline_dir' (if exists)..." >&2;
            execute mkdir -p "$renamed_artifacts_dir"
            execute mv "$summaries_dir" "$renamed_artifacts_dir"
            execute mv "$results_dir" "$renamed_artifacts_dir"
            execute mv "$ARTIFACTS_DIR/*.log" "$renamed_artifacts_dir"
            ;;
        3)  echo "Delete the contents of the directory, except for the base line '$baseline_dir'";
            execute rm -rf "$summaries_dir"
            execute rm -rf "$results_dir"
            execute rm -rf "$ARTIFACTS_DIR/*.log"
            ;;
        4)  echo "Exiting the script.";
            exit 0
            ;;
        *)  echo "Invalid option $choice. Exiting." >&2;
            exit 2
            ;;
    esac
fi

trace "Creating directory(s)..."
execute mkdir -p "$summaries_dir"

trace "Running benchmark tests in project '$bm_project' with configuration '$configuration'..."
execute mkdir -p "$ARTIFACTS_DIR"
execute dotnet run \
    /p:DefineConstants="$define" \
    --project "$bm_project" \
    --configuration "$configuration" \
    --filter '*' \
    --memory \
    --exporters JSON \
    --artifacts "$ARTIFACTS_DIR"

if ! command -v jq >"$_ignore" 2>&1; then
    execute sudo apt-get update && sudo apt-get install -y jq
    echo "jq successfully installed."
fi

if [[ $dry_run != "true" ]]; then

    declare -a files

    mapfile -t -d " " files < <(list_of_files "$ARTIFACTS_DIR/results/*-report.json")

    if [[ ${#files[@]} == 0 ]]; then
        echo "No JSON reports found." >&2
        exit 2
    fi
    for f in "${files[@]}"; do
        sf=$(sed -nE 's/(.*)-report.json/\1-summary.json/p' <<< "$(basename "$f")")
        jq -f "$solution_dir/.github/workflows/summary.jq" "$f" > "$summaries_dir/$sf"
    done
fi

trace "Sum up the means from all the summary files"
mapfile -t -d " " files < <(list_of_files "$summaries_dir/*-summary.json")
if [[ ${#files[@]} == 0 ]]; then
    echo "No current benchmark result JSON files found." >&2
    exit 2
fi
sum_cur=0
for f in "${files[@]}"; do
    VAL=$(jq '( .Totals.Mean // 0)' "$f")
    sum_cur=$(( sum_cur + VAL ))
done
if (( sum_cur == 0 )); then
    echo "Current sum is invalid ($sum_cur)." >&2
    exit 2
fi

trace "Sum up the means from all the baseline summary files"
mapfile -t -d " " files < <(list_of_files "$baseline_dir/*-summary.json")
if [[ ${#files[@]} == 0 ]]; then
    echo "Baseline reports were not found at $baseline_dir." >&2
    if is_defined "GITHUB_ENV"; then
        echo "Creating a new baseline from the current results."
        # shellcheck disable=SC2154
        echo "FORCE_NEW_BASELINE=true" >> "$GITHUB_ENV"
    fi
    exit 0
fi
sum_base=0
for f in "${files[@]}"; do
    VAL=$(jq '( .Totals.Mean // 0)' "$f")
    sum_base=$(( sum_base + VAL ))
done
if (( sum_base == 0 )); then
    echo "Baseline sum is invalid ($sum_base)." >&2
    exit 2
fi

trace "Calculating the percent change vs baseline"
pct=$(( (sum_cur - sum_base) * 100 / sum_base ))
echo "Percent change vs baseline: $pct% (allowed: $max_regression_pct%)"
flush_stdout

if (( pct > max_regression_pct )); then
    echo "Performance regression exceeds threshold" >&2
    if [[ $force_new_baseline == "true" ]] && is_defined "GITHUB_ENV"; then
        echo "Significant regression of $pct% over baseline. Updating the baseline."
        # shellcheck disable=SC2154
        echo "FORCE_NEW_BASELINE=true" >> "$GITHUB_ENV"
        exit 0
    fi
    echo "If this is acceptable, please update the baseline by setting the variable 'FORCE_NEW_BASELINE=true'." >&2
    exit 2
elif (( pct > 0 )); then
    echo "Performance regression within acceptable threshold."
    flush_stdout
elif (( pct < 0 )); then
    pct_abs=$(( -pct ))
    if (( pct_abs >= max_regression_pct )); then
        if is_defined "GITHUB_ENV"; then
            echo "Significant improvement of $pct_abs% over baseline. Updating the baseline."
            # shellcheck disable=SC2154
            echo "FORCE_NEW_BASELINE=true" >> "$GITHUB_ENV"
        fi
    else
        echo "Improvement of $pct_abs% over baseline."
    fi
    flush_stdout
fi
