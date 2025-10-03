#!/bin/bash

# This script defines a number of general purpose functions.
# For the functions to be invocable by other scripts, this script needs to be sourced.
# When fatal parameter errors are detected, the script invokes exit, which leads to exiting the current shell.

# commonly used variables
initial_dir=$(pwd)
declare -xr initial_dir

_ignore=${_ignore:="/dev/null"}

declare -x trace_enabled=${TRACE_ENABLED:-false}
declare -x verbose=${VERBOSE:-false}
declare -x debugger=${DEBUGGER:-false}
declare -x dry_run=${DRY_RUN:-false}
declare -x quiet=${QUIET:-false}
declare -x ci=${CI:-false}

[[ "$ci" == "true" ]] && quiet=true

declare last_command
declare current_command="$BASH_COMMAND"
# on_debug when specified as a handler of the DEBUG trap, remembers the last invoked bash command in $last_command.
# on_debug and on_exit are trying to cooperatively do error handling when exit is invoked. To be effective, after
# sourcing this script, set these signal traps:
#   trap on_debug DEBUG
#   trap on_exit EXIT
function on_debug() {
    # keep track of the last executed command
    last_command="$current_command"
    current_command="$BASH_COMMAND"
}

# on_exit when specified as a handler of the EXIT trap
#   * if on_debug handles the DEBUG trap, displays the failed command
#   * if $initial_dir is defined, changes the current working directory to it
#   * does `set +x`.
# on_debug and on_exit are trying to cooperatively do error handling when exit is invoked. To be effective, after
# sourcing this script, set these signal traps:
#   trap on_debug DEBUG
#   trap on_exit EXIT
function on_exit() {
    # echo an error message before exiting
    local x=$?
    if (( x != 0)); then
        echo "'$last_command' command failed with exit code $x" >&2
    fi
    if [[ -n "$initial_dir" ]]; then
        cd "$initial_dir" || exit
    fi
    set +x
}

function trace() {
    if [[ "$trace_enabled" == true ]]; then
        echo "Trace: $*" | tee /dev/null
    fi
}

# Depending on the value of $dry_run either executes or just displays what would have been executed.
function execute() {
    if [[ "$dry_run" == "true" ]]; then
        echo "dry-run$ $*" | tee /dev/null
        return 0
    fi
    trace "$*"
    "$@" > "$_ignore"
    return $?
}

# to_lower converts all characters in the passed in value to lowercase and prints the to stdout.
# Usage example: local a="$(to_lower "$1")"
function to_lower() {
    printf "%s" "${1,,}"
}

# to_upper converts all characters in the passed in value to uppercase and prints the to stdout.
# Usage example: local a="$(to_upper "$1")"
function to_upper() {
    printf "%s" "${1^^}"
}

# is_positive tests if its parameter represents a valid positive, integer number (aka natural number): {1, 2, 3, ...}
function is_positive() {
    [[ "$1" =~ ^[+]?[0-9]+$  && ! "$1" =~ ^[+]?0+$ ]]
}

# is_non_negative tests if its parameter represents a valid non-negative integer number: {0, +0, 1, 2, 3, ...}
function is_non_negative() {
    [[ "$1" =~ ^[+]?[0-9]+$ ]]
}

# is_non_positive tests if its parameter represents a valid non-positive integer number: {0, -0, -1, -2, -3, ...}
function is_non_positive() {
    [[ "$1" =~ ^-[0-9]+$ || "$1" =~ ^[-]?0+$ ]]
}

# is_negative tests if its parameter represents a valid negative integer number: {-1, -2, -3, ...}
function is_negative() {
    [[ $1 =~ ^-[0-9]+$ && ! "$1" =~ ^[-]?0+$ ]]
}

# is_integer tests if its parameter represents a valid integer number: {..., -2, -1, 0, 1, 2, ...}
function is_integer() {
    [[ "$1" =~ ^[-+]?[0-9]+$ ]]
}

# is_decimal tests if its parameter represents a valid decimal number
function is_decimal() {
    [[ "$1" =~ ^[-+]?[0-9]*(\.[0-9]*)?$ ]]
}

# is_in tests if the first parameter is equal to one of the following parameters.
function is_in() {
    if [[ $# -lt 2 ]]; then
        echo "The function is_in() requires at least 2 arguments: the value to test and at least one valid option." >&2
        return 2
    fi

    local sought="$1"; shift
    local v
    for v in "$@"; do
        [[ "$sought" == "$v" ]] && return 0
    done
    return 1
}

function is_defined() {
    if [[ $# -ne 1 ]]; then
        echo "The function is_defined() requires exactly one argument: the name of the variable to test." >&2
        return 2
    fi

    if [[ -v "$1" ]] && declare -p "$1" > "$_ignore"; then
        return 0
    else
        return 1
    fi
}

function flush_stdout() {
    printf "" | tee > /dev/null
    return 0
}

function list_of_files() {
    if [[ $# -lt 1 ]]; then
        echo "The function list_ofFiles() requires at least one parameter: the file pattern." >&2
        return 2
    fi

    local pattern="$1"

    # by default, if a glob pattern does not match any files, it expands to an empty string instead of the default to leaving
    # the pattern unchanged, e.g. ${ARTIFACTS_DIR}/results/*-report.json - we don't want that
    shopt -s nullglob
    shopt -s globstar || true
    # shellcheck disable=SC2206
    local list=($pattern)
    shopt -u nullglob
    shopt -u globstar || true
    printf "%s" "${list[*]}"
    return 0
}

# confirm asks the script user to respond yes or no to some prompt. If there is a defined variable $quiet with
# value "true", the function will not display the prompt and will assume the default response or 'y'.
# Parameter 1 - the prompt to confirm.
# Parameter 2 - the default response if the user presses [Enter]. When specified should be either 'y' or 'n'. Optional.
# Outputs the result to stdout as 'y' or 'n'.
function confirm() {
    if [[ -z "$1" ]]; then
        echo "The function confirm() requires at least one parameter: the prompt." >&2
        exit 2
    fi
    if [[ -n "$2" && ! "$2" =~ ^[ynYN]$ ]]; then
        echo "If a default response parameter is specified for the function confirm(), it must be either 'y' or 'n'" >&2
        exit 2
    fi

    local default
    local prompt="$1"

    default=$(to_lower "${2:-y}")

    if [[ "$quiet" == true ]]; then
        print '%s' "$default"
        return 0
    fi

    local suffix

    if [[ "$default" == y ]]; then
        suffix="[Y/n]"
    else
        suffix="[y/N]"
    fi

    local response
    while true; do
        read -rp "$prompt $suffix: " response >&2
        response=${response:-$default}
        response=${response,,}
        if [[ "$response" =~ ^[yn]$ ]]; then
            printf '%s' "$response"
            return 0;
        fi
        echo "Please enter y or n." >&2
    done
}

# choose displays a prompt and a list of options to the script user and asks them to choose one of the options.
# Parameter 1 - the prompt to display before the options.
# Parameter 2 - the text of the first option.
# Parameter 3 - the text of the second option.
# Parameter 4, ... - the text of more options.
# The first option is the default one.
# The result will be printed in stdout as the number of the chosen option.
# The function will exit with code 2 if less than 3 parameters are specified.
function choose() {
    if [[ $# -lt 3 ]]; then
        echo "The function choose() requires 3 or more arguments: a prompt and at least two choices." >&2;
        return 2;
    fi

    local prompt=$1; shift
    local options=("$@")

    if [[ "$quiet" == true ]]; then
        printf '1'
        return 0
    fi

    echo "$prompt" >&2
    local -i i=1
    for o in "${options[@]}"; do
        if [[ $i -eq 1 ]]; then
            echo "  $i) $o (default)" >&2
        else
            echo "  $i) $o" >&2
        fi
        ((i++))
    done

    local -i selection=1
    while true; do
        read -rp "Enter choice [1-${#options[@]}]: " selection
        selection=${selection:-1}
        [[ $selection = 0 ]] && selection=1
        if [[ $selection =~ ^[1-9][0-9]*$ && $selection -ge 1 && $selection -le ${#options[@]} ]]; then
            printf '%d' "$selection"
            return 0
        fi
        echo "Invalid choice: $selection" >&2
    done
    return 0
}

declare return_userid
declare return_passwd
# get_credentials gets a user ID and a password from the script user. In the end the script will ask the user to
# confirm their entries.
# Parameter 1 - the prompt for getting the user ID. Default "Enter the user ID: "
# Parameter 2 - the prompt for getting the password. Default "Enter the user password: "
# Parameter 3 - the prompt to confirm that the input is correct. If empty the function will not ask the user to confirm
# their input.
# The user ID and the password will be held in $return_userid and $return_passwd until the next invocation of the
# function.
function get_credentials() {
    local promptUserID=${1:-"Enter the user ID: "}
    local promptPassword=${2:-"Enter the password: "}
    local promptConfirm=$3
    return_userid=""
    return_passwd=""
    until [[ -n $return_userid  &&  -n $return_passwd ]]; do
        read -rp "$promptUserID" return_userid >&2
        read -rsp "$promptPassword" return_passwd >&2
        echo >&2
        if [[ -n "$promptConfirm" && $(confirm "$promptConfirm") != "y" ]]; then
            return_userid=""
            return_passwd=""
        fi
    done
}

# get_from_yaml gets the result of executing JSON query expression on YAML file.
# Requires "yq".
# Param 1 - the JSON query expression to execute
# Param 2 - the YAML file
# Return 0 if the result is not null and not empty; otherwise 1
# The query result will be output to stdout.
function get_from_yaml()
{
    if [[ $# -lt 2 ]]; then
        echo "The function get_from_yaml() requires two parameters: the query and the yaml file name." >&2
        return 2
    fi

    local query="$1";
    local file="$2"
    if [[ ! -s "$file" ]]; then
        echo "The file '$file' is empty or does not exist." >&2
        return 1
    fi
    local r
    r=$(yq eval "$query" "$file") || return 1
    [[ "$r" == "null" ]] && return 1
    printf '%s' "$r"
    return 0
}

# press_any_key displays a prompt, followed by "Press any key to continue..." and returns only after the script user
# presses a key. If there is defined variable $quiet with value "true", the function will not display prompt and will
# not wait for response.
function press_any_key() {
    if [[ "$quiet" != true ]]; then
        read -n 1 -rsp 'Press any key to continue...' >&2
        echo
    fi
}

# Dumps a table of variables and in the end asks the user to press any key to continue.
# The names of the variables must be specified as strings - no leading $.
# Optionally the caller can specify flags like:
# -h or --header <text> will display the header text and the dividing lines in the table
# -b or --blank will display an empty line in the table
# -l or --line will display a dividing line
# -q or --quiet will not ask the user to press any key to continue after dumping the variables
function dump_vars() {
    if [[ $# -eq 0 || $trace_enabled != true ]]; then
        return;
    fi

    local top=true
    local decl
    local quiet=false
    echo "┌───────────────────────────────────────────────────────────"; flush_stdout
    until [[ $# = 0 ]]; do
        case $1 in
            -h|--header )
                shift
                if [[ $top != "true" ]]; then
                    echo "├───────────────────────────────────────────────────────────"
                fi
                echo "│ $1"
                echo "├───────────────────────────────────────────────────────────"
                ;;

            -b|--blank ) echo "│" ;;

            -l|--line ) echo "├───────────────────────────────────────────────────────────" ;;

            -q|--quiet ) quiet=true ;;

            * ) write_line "$1" ;;
        esac
        shift
        top=false
    done
    echo "└───────────────────────────────────────────────────────────"
    flush_stdout
    [[ "$quiet" == true ]] || press_any_key
}



function write_line() {
    local -n v="$1"
    local decl
    if is_defined "$1"; then
        decl="$(declare -p "$1" 2> /dev/null)"
        if [[ $decl =~ 'declare -a' ]]; then
            printf "│ \$%-40s%s\n" "$1" "${#v[@]}: (${v[*]})"
        else
            printf "│ \$%-40s%s\n" "$1" "$v"
        fi
        unset -n v
    else
        printf "│ \$%-40s%s\n" "$1" "****** unbound, undefined, or invalid"
    fi
    return 0
}

# scp_retry tries the SSH copy command up to three times with timeout of 10sec timeout between retries.
# Parameters - the same parameters as the scp command, this is there must be at least 2 arguments.
# If the operation is successful it will set $return_copied to 'true' until the next invocation.
function scp_retry() {
    local -i try=0
    local -i retry_after=10
    while true; do
        if execute scp "$@"; then
            printf 'true'
            return 0
        fi
        if ((try < 3)); then
            echo "  - will try scp again in ${retry_after}sec..." >&2
            sleep "$retry_after"
        else
            break
        fi
        ((try++))
    done

    echo "FAILED: scp $*" >&2
    return 1
}

# --- test harness' assertion helpers -------------------------------------------------------

PASS=0
FAIL=0
TOTAL=0

function fail() {
  echo "[FAIL] $1" >&2
  exit 1;
}

function assert_eq() {
  local exp=$1 got=$2 msg=${3:-}
  (( TOTAL++ ))
  if [[ "$exp" == "$got" ]]; then
    (( PASS++ ))
    ([[ $VERBOSE == true ]] && echo "[OK] ${msg:-eq}" >&2) || true
  else
    (( FAIL++ ))
    echo "[FAIL] ${msg:-eq}: expected='$exp' got='$got'" >&2
    exit 1
  fi
}

function assert_true() {
  local msg=$1; shift || true
  (( TOTAL++ ))
  if "$@"; then
    (( PASS++ ))
    ([[ $VERBOSE == true ]] && echo "[OK] $msg" >&2) || true
  else
    (( FAIL++ ))
    echo "[FAIL] $msg" >&2
    exit 1
  fi
}

assert_false() {
  local msg=$1; shift || true
  (( TOTAL++ ))
  if "$@"; then
    (( FAIL++ ))
    echo "[FAIL] $msg (expected false)" >&2
    exit 1
  else
    (( PASS++ ))
    ([[ $VERBOSE == true ]] && echo "[OK] $msg" >&2) || true
  fi
}
