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

declare -x SUMMARIES_DIR=${SUMMARIES_DIR:="$ARTIFACTS_DIR/summaries"}
SUMMARIES_DIR=$(realpath -m "$SUMMARIES_DIR")  # ensure it's an absolute path

declare configuration=${CONFIGURATION:="Release"}

source "$script_dir/_common.sh"
source "$script_dir/run-benchmarks-utils.sh"

get_arguments "$@"
declare -r bm_project
declare -r configuration

dump_all_variables

trace "Creating directory(s)..."
execute mkdir -p "$SUMMARIES_DIR"

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
        files=("${ARTIFACTS_DIR}/results/*-report.json")
    shopt -u nullglob

    if [ ${#files[@]} -eq 0 ]; then
        echo "No JSON reports found." >&2
        exit 2
    fi

    for f in "${files[@]}"; do
        sf=$(sed -nE 's/(.*)-report.json/\1-summary.json/p' <<< "$(basename "${f}")")
        jq -f "$solution_dir/.github/workflows/summary.jq" "${f}" > "${SUMMARIES_DIR}/${sf}"
    done
fi
