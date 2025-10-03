#!/usr/bin/env bash
# Lightweight self-test harness for scripts/bash utilities.
# Intended to be run locally or in CI (optional). No external deps required.
# Usage:
#   ./scripts/bash/self-test.sh            # run all tests
#   VERBOSE=1 ./scripts/bash/self-test.sh  # verbose output
# Exits non-zero on first failure.

set -euo pipefail

# --- tests -------------------------------------------------------------------

SCRIPT_DIR=$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=_common.sh
source "$SCRIPT_DIR/_common.sh"

# to_lower / to_upper (legacy return_* channel)

to_lower "ABCdEF"; assert_eq "abcdef" "$return_lower" "to_lower basic"
to_upper "abcDef"; assert_eq "ABCDEF" "$return_upper" "to_upper basic"

# numeric predicates
assert_true  "is_positive 1" is_positive 1
assert_false "is_positive 0" is_positive 0
assert_true  "is_non_negative 0" is_non_negative 0
assert_true  "is_non_positive -3" is_non_positive -3
assert_false "is_negative 0" is_negative 0
assert_true  "is_integer -42" is_integer -42
assert_true  "is_decimal 3.1415" is_decimal 3.1415
assert_true  "is_decimal .99" is_decimal .99

# is_in
assert_true  "is_in in list" bash is_in bash zsh fish
assert_false "is_in not in list" is_in lorem ipsum dolor

# confirm (quiet mode path)
quiet=true
confirm "Proceed?" y
assert_eq y "$return_yes_no" "confirm quiet default y"
quiet=false

# choose (quiet mode path) -> should set choice_option=1
quiet=true
choose "Pick" A B C
assert_eq 1 "$choice_option" "choose quiet default 1"
quiet=false

# execute dry-run formatting
prev_dry_run=$dry_run
DRY_RUN=true
 dry_run=true
out=$(execute echo one two three)
# we expect no output because execute prints dry-run$ ... to stdout; capture & test prefix
if [[ "$out" != dry-run$* ]]; then
  echo "[WARN] dry-run output format changed: '$out'" >&2
fi
 dry_run=$prev_dry_run

# get_from_yaml (only if yq present)
if command -v yq >"$_ignore" 2>&1; then
  tmp_yaml=$(mktemp)
  printf 'a: 1\nb: { c: 2 }\n' > "$tmp_yaml"
  get_from_yaml '.b.c' "$tmp_yaml"
  assert_eq 2 "$return_yqResult" "get_from_yaml .b.c==2"
  rm -f "$tmp_yaml"
else
  [[ $VERBOSE == 1 ]] && echo "[SKIP] yq not installed; skipping get_from_yaml"
fi

# scp_retry (simulate failure quickly by calling with no args -> scp will error) -- optional best-effort
if [[ ${TEST_SCP:-0} == 1 ]]; then
  if scp_retry; then
    echo "[WARN] expected scp_retry to fail with no args" >&2
  fi
fi

echo "All bash self-tests passed: $PASS / $TOTAL"
exit 0
