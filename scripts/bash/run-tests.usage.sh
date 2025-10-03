#!/bin/bash

function usage_text()
{
    # shellcheck disable=SC2154 # solution_dir is referenced but not assigned.
    cat << EOF
Usage:

    ${script_name} [<test-project-path>] |
        [--<long option> <value>|-<short option> <value> |
         --<long switch>|-<short switch> ]*

    This script runs the tests in the specified test project and collects code
    coverage information. It assumes that the directory 'scripts/bash' is
    located in the root directory of the solution (here: $solution_dir).

Parameters:
    <test-project-path>
        The path to the test project file. Optional. Initial value from
        \$TEST_PROJECT or $test_project

Switches:$common_switches

Options:
    --min-coverage-pct | -t
        Specifies the minimum acceptable code coverage percentage (0-100).
        Initial value from \$MIN_COVERAGE_PCT or 80

    --artifacts | -a
        Specifies the directory where to create the script's artifacts: summary,
        report files, etc.
        Initial value from \$ARTIFACTS_DIR or '\$solution_dir/TestArtifacts'
        ($solution_dir/TestArtifacts)

    --configuration | -c
        Specifies the build configuration to use ('Debug' or 'Release').
        Initial value from \$CONFIGURATION or 'Release'

    --define | -d
        Defines one or more user-defined pre-processor symbols to be used when
        building the benchmark project, e.g. 'STAGING'. You can specify this
        option multiple times to define multiple symbols.
        Initial value from \$DEFINE or ''

EOF
}

function usage()
{
    display_usage_msg "$(usage_text)" "$@"
}
