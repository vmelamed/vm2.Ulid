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

Switches:
    --debugger
            Set when the script is running under a debugger, e.g. 'gdb'. If
            specified, the script will not set traps for DEBUG and EXIT, and
            will set the '--quiet' switch. If needed, this option must be
            specified as the first argument.
            Initial value from \$DEBUGGER or 'false'

    --help | -h | -?
            Displays this usage text and exits.

    --dry-run | -y
            Runs the script without executing any commands but shows what would
            have been executed.
            Initial value from \$DRY_RUN or 'false'

    --quiet | -q
            Suppresses all prompts for input from the user, and assumes the
            default answers.
            Initial value from \$QUIET or 'false'

    --verbose | -v
            Enables verbose output: all output from the invoked commands (e.g.
            dotnet, reportgenerator) to be sent to 'stdout' instead of
            '/dev/null'. It also enables the output from the script function
            trace().
            Initial value from \$VERBOSE or 'false'

    --trace | -x
            Sets the Bash trace option 'set -x' and enables the output from the
            functions 'trace' and 'dump_vars'.
            Initial value from \$TRACE_ENABLED or 'false'

Options:
    --min-coverage-pct | -t
            Specifies the minimum acceptable code coverage percentage (0-100).
            Initial value from \$MIN_COVERAGE_PCT or 80

    --artifacts | -a
            Specifies the directory where to create the script's artifacts:
            summary, report files, etc.
            Initial value from \$ARTIFACTS_DIR or '\$solution_dir/TestArtifacts'
            ($solution_dir/TestArtifacts)

    --configuration | -c
            Specifies the build configuration to use ('Debug' or 'Release').
            Initial value from \$CONFIGURATION or 'Release'

    --define | -d
            Defines one or more user-defined pre-processor symbols to be used
            when building the benchmark project, e.g. 'STAGING'. You can
            specify this option multiple times to define multiple symbols.
            Initial value from \$DEFINE or ''
EOF
}

function usage()
{
    display_usage_msg "$(usage_text)" "$@"
}
