#!/bin/bash
set -euo pipefail

this_script=${BASH_SOURCE[0]}
declare -xr this_script

script_name="$(basename "${this_script%.*}")"
declare -xr script_name

script_dir="$(dirname "$(realpath -e "$this_script")")"
declare -xr script_dir

source "$script_dir/_common.sh"

