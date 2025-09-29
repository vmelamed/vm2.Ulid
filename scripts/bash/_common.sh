# #!/bin/bash

# This script defines a number of general purpose functions.
# For the functions to be invokable by other sripts, this script needs to be sourced.
# When fatal parameter errors are detected, the script invokes exit, which leads to exiting the current shell.

# commonly used variables
readonly initial_directory=$(pwd)
readonly timestamp=$(date +%Y%m%d-%H%M%S)

_output=${_output:="/dev/null"}
debug=${DEBUG:-false}
trace=${TRACE:-false}
dry_run=${DRY_RUN:-false}
quiet=${QUIET:-false}
debugger=${DEBUGGER:-false}

declare last_command
declare current_command="${BASH_COMMAND}"
# on_debug when specified as a handler of the DEBUG trap, remembers the last invoked bash command in ${last_command}.
# on_debug and on_exit are trying to cooperatively do error handling when exit is invoked. To be effective, after
# sourcing this script, set these signal traps:
#   trap on_debug DEBUG
#   trap on_exit EXIT
function on_debug() {
    # keep track of the last executed command
    last_command="${current_command}"
    current_command="${BASH_COMMAND}"
}

# on_exit when specified as a handler of the EXIT trap
#   * if on_debug handles the DEBUG trap, displays the failed command
#   * if ${initial_directory} is defined, changes the current working directory to it
#   * does `set +x`.
# on_debug and on_exit are trying to cooperatively do error handling when exit is invoked. To be effective, after
# sourcing this script, set these signal traps:
#   trap on_debug DEBUG
#   trap on_exit EXIT
function on_exit() {
    # echo an error message before exiting
    local x=$?
    if [[ ! $x ]]; then
        echo "\"${last_command}\" command failed with exit code $x"
    fi
    if [[ "${initial_directory}" ]]; then
        cd "${initial_directory}"
    fi
    set +x
}

function trace() {
    if [[ $debug ]]; then
        echo "Trace: $@" 1>&2
    fi
}

# Depending on the value of $dry_run either executes or just displays what would have been executed.
function execute()
{
    if [[ $dry_run == "true" ]]; then
        echo "dry-run$ $@"
        return 0
    else
        trace "$@"
        eval "$@" && return 0 || return $?
    fi
}

declare return_lower
# to_lower converts all characters in the passed in value to lowercase.
# The result will be held in ${return_lower} until the next invocation of the function.
function to_lower() {
    return_lower="$(echo "${1}" | awk '{print tolower($0)}')"
}

declare return_upper
# to_upper converts all characters in the passed in value to lowercase.
# The result will be held in ${return_upper} until the next invocation of the function.
function to_upper() {
    return_upper="$(echo "${1}" | awk '{print toupper($0)}')"
}

# is_positive tests if its parameter represents a valid positive, integer number (aka natural number): {1, 2, 3, ...}
function is_positive() {
    if [[ "${1}" =~ ^[0-9]+$  && ! "${1}" =~ ^0+$ ]]; then
        return 0
    else
        return 1
    fi
}

# is_non_negative tests if its parameter represents a valid non-negative integer number: {0, 1, 2, 3, ...}
function is_non_negative() {
    if [[ "${1}" =~ ^[0-9]+$ ]]; then
        return 0
    else
        return 1
    fi
}

# is_non_positive tests if its parameter represents a valid non-positive integer number: {0, -1, -2, -3, ...}
function is_non_positive() {
    if [[ "${1}" =~ ^-[0-9]+$ || "${1}" =~ ^0+$ ]]; then
        return 0
    else
        return 1
    fi
}

# is_negative tests if its parameter represents a valid negative integer number: {-1, -2, -3, ...}
function is_negative() {
    [[ ${1} =~ ^-[0-9]+$ && ! "${1}" =~ ^-0+$ ]] && return 0 || return 1
}

# is_integer tests if its parameter represents a valid integer number: {..., -2, -1, 0, 1, 2, ...}
function is_integer() {
    if [[ "${1}" =~ ^[-+]?[0-9]+$ ]]; then
        return 0
    else
        return 1
    fi
}

# is_decimal tests if its parameter represents a valid decimal number
function is_decimal() {
    if [[ "${1}" =~ ^[-+]?[0-9]*(\.[0-9]*)?$ ]]; then
        return 0
    else
        return 1
    fi
}

# is_in tests if the first parameter is equal to one of the following parameters.
function is_in() {
    if [[ $# < 2 ]]; then
        echo "At least 2 parameters required"
        return 2
    fi

    sought="$1"
    shift

    for v in "$@"; do
        if [[ "${sought}" == "${v}" ]]; then
            return 0
        fi
    done
    return 1
}

declare return_yes_no
# confirm asks the script user to answer yes or no to some prompt. If there is a defined variable ${quiet} with
# value "true", the function will not display the prompt and will assume the default answer. If no default answer is
# specified, it will assume answer 'y'.
# Parameter 1 - the prompt to confirm.
# Parameter 2 - the default answer if the user presses [Enter]. When specified should be either 'y' or 'n'. Optional.
# Returns 0, if the reply is 'y'; otherwise 1.
# The result will be held in ${return_yes_no} as 'y' or 'n' until the next invocation of the function.
function confirm() {
    if [[ ! "${1}" ]]; then
        echo "Parameter 1 - prompt - is mandatory"
        exit 1
    fi

    local prompt="${1}"
    to_lower "${2}"
    local default="${return_lower}"
    if [[ "${default}" && "${default}" != 'y' && "${default}" != 'n' ]]; then
        echo "Parameter 2 - default answer - if specified must be either 'y' or 'n'"
        exit 1
    fi

    if [[ "${quiet}" == "true" ]]; then
        return_yes_no="${default:-y}"
        return 0
    fi

    return_yes_no=""

    local p=""
    until [[ "${return_yes_no}" == "y"  ||  "${return_yes_no}" == "n" ]]; do
        if [[ "${default}" == "y" ]]; then
            p="${prompt} [Y/n]: "
        elif [[ "${default}" == "n" ]]; then
            p="${prompt} [y/N]: "
        else
            p="${prompt} [y/n]: "
        fi
        read -p "${p}" -n 1 return_yes_no
        echo

        to_lower "${return_yes_no}"
        return_yes_no="${return_lower}"
        if [[ ! "${return_yes_no}" ]]; then
            return_yes_no="${default}"
        fi
    done
    if [[ "${return_yes_no}" == 'y' ]]; then
        return 0;
    else
        return 1;
    fi
}

declare -i choice_option
# choose displays a prompt and a list of options to the script user and asks them to choose one of the options.
# Parameter 1 - the prompt to display before the options.
# Parameter 2 - the text of the first option.
# Parameter 3 - the text of the second option.
# Parameter 4, ... - the text of more options.
# The first option is the default one.
# The result will be held in ${choice_option} as the number of the chosen option until the next invocation of the function.
# The function will exit with code 1 if less than 3 parameters are specified.
function choose() {
    if [[ $# < 3 ]]; then
        echo "choose <Parameter1> (prompt), <Parameter2> (option1), <Parameter3> (option2) are mandatory. You can specify more options."
        exit 1
    fi

    if [[ "${quiet}" == "true" ]]; then
        choice_option=1
        return 1
    fi

    choice_option=0

    local i=1
    local options=()

    echo "${1}"; shift
    for o in "$@"; do
        if (($i == 1)); then
            echo "  ${i}. ${o} (the default!)"
        else
            echo "  ${i}. ${o}"
        fi
        options+=("$i")
        let i++
    done

    local ch

    until is_positive "${ch}" && (( ch >= 1 && ch <= ${#options[@]} )); do
        read -p "Enter ch [1-${#options[@]}]: " ch
        if [[ ! "${ch}" ]]; then
            ch=1
        elif ! is_positive "${ch}" || (( ch < 1 || ch > ${#options[@]} )); then
            echo "Invalid ch: ${ch}"
        fi
    done

    choice_option=$((ch + 0))    # ensure it's an integer
}

declare return_userid
declare return_passwd
# get_credentials gets a user ID and a password from the script user. In the end the scfunction will ask the user to
# confirm their entries.
# Parameter 1 - the prompt for getting the user ID. Default "Enter the user ID: "
# Parameter 2 - the prompt for getting the password. Default "Enter the user password: "
# Parameter 3 - the prompt to confirm that the input is correct. If empty the function will not ask the user to confirm
# their input.
# The user ID and the password will be held in ${return_userid} and ${return_passwd} until the next invocation of the
# function.
function get_credentials() {
    local promptUserID=${1:-"Enter the user ID: "}
    local promptPassword=${2:-"Enter the password: "}
    local promptConfirm=${3}
    return_userid=""
    return_passwd=""
    until [[ ${return_userid}  &&  ${return_passwd} ]]; do
        read -p "${promptUserID}" return_userid
        read -p "${promptPassword}" -s return_passwd
        echo
        if [[ "${promptConfirm}" ]]; then
            confirm "${promptConfirm}" "y"
            if [[ "${return_yesNo}" != "y" ]]; then
                return_userid=""
                return_passwd=""
            fi
        fi
    done
}

declare return_yqResult
# get_from_yaml gets the result of executing JSON query expression on YAML file.
# Requires "yq".
# Param 1 - the JSON query expression to execute
# Param 2 - the YAML file
# Return 0 if the result is not null and not empty; otherwise 1
# The query result will be held in the variable ${return_yqResult} until the next invokation of the function.
function get_from_yaml()
{
    return_yqResult=""
    if [[ -s $1 ]]; then
        local r=$(yq eval $1 $2)

        if [[ $r != "null" ]]; then
            return_yqResult="$r"
            return [[ -n ${return_yqResult} ]]
        else
            return 1
        fi
    fi
}

# press_any_key displays a prompt, followed by "Press any key to continue..." and returns only after the script user
# presses a key. If there is defined variable ${quiet} with value "true", the function will not display prompt and will
# not wait for response.
function press_any_key() {
    if [[ "${quiet}" != "true" ]]; then
        read -n 1 -r -s -p $'Press any key continue...\n'
    fi
}

readonly ul_corner="┌"
readonly ll_corner="└"
readonly h_line="─"
readonly v_line="│"
readonly vh_line="├"

# Dumps a table of variables and in the end asks the user to press any key to continue.
# The variables are specified by name only - no leading $. Optionally the caller can specify flags like:
# -h or --header will display a header string and dividing line in the table
# -b or --blank will display an empty line in the table
# -l or --line will display a dividing line
# -q or --quiet will suppress the "Press any key to continue..." prompt.
function dump_vars() {
    if [[ "$#" -eq 0 || $quiet ]]; then
        return;
    fi

    echo "┌───────────────────────────────────────────────────────────"
    local top="true"
    until [[ $# = 0 ]]; do
        case $1 in
            -b|--blank )
                echo "│"
                ;;
            -h|--header )
                shift
                if [[ $top != "true" ]]; then
                echo "├───────────────────────────────────────────────────────────"
                fi
                echo "│ ${1}"
                echo "├───────────────────────────────────────────────────────────"
                ;;
            -l|--line )
                echo "├───────────────────────────────────────────────────────────"
                ;;
            * )
                printf "│ "
                if echo "$1" | grep -qiE '^[_a-z][_a-z0-9]*$'; then
                    if ! declare -p $1 &> /dev/null; then
                        printf "\$%-40s\"\"\n" "$1"
                    elif echo "$(declare -p $1)" | grep -q "declare -a" ; then
                        eval "printf" "\$%-40s%s\\\n" "$1" "\"(\${$1[*]})\""
                    else
                        eval "printf" "\$%-40s%s\\\n" "$1" "\"\$$1\""
                    fi
                else
                    printf "\$%-40s???\n" "$1"
                fi
                ;;
        esac
        shift
        top=""
    done
    echo "└───────────────────────────────────────────────────────────"
    press_any_key
}

declare return_copied
# scp_retry tries the SSH copy command up to three times with timeout of 10sec timeout between retries.
# Parameters - the same parameters as the scp command, this is there must be at least 2 parameters.
# If the operation is successful it will set ${return_copied} to 'true' until the next invocation.
function scp_retry() {
    local try=0
    local retry_after=10
    return_copied="false"
    until [[ ${return_copied} == "true" || $try -ge 3 ]]; do
        if execute scp "$@"; then
            return_copied="true"
            return 0
        fi
        if [[ $i -lt 3 ]]; then
            echo "  - will try scp again in ${retry_after}sec..."
            sleep retry_after
        fi
        let try++
    done

    echo "FAILED: scp $@"
    return 1
}
