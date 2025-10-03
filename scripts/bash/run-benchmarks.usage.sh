#!/bin/bash

function usage_text()
{
    # shellcheck disable=SC2154 # solution_dir is referenced but not assigned.
    cat << EOF
Usage:

    ${script_name} [<bm-project-path>] |
       [--<long option> <value> | -<short option> <value> |
        --<long switch> | -<short switch> ]*

    This script runs the benchmark tests in the specified project. It assumes
    that the directory 'scripts/bash' is located in the root directory of the
    solution (here: $solution_dir).

Parameters:
    <bm-project-path>
        The path to the benchmark project file. Optional.
        Initial value from \$BM_PROJECT or $bm_project

Switches:
    --debugger
        Set when the script is running under a debugger, e.g. 'gdb'. If
        specified, the script will not set traps for DEBUG and EXIT, and will
        set the '--quiet' switch. If needed, this option must be specified as
        the first argument.
        Initial value from \$DEBUGGER or 'false'

    --help | -h | -?
        Displays this usage text and exits.

    --dry-run | -y
        Runs the script without executing any commands but shows what would have
        been executed.
        Initial value from \$DRY_RUN or 'false'

    --quiet | -q
        Suppresses all prompts for input from the user, and assumes the default
        answers.
        Initial value from \$QUIET or 'false'

    --verbose | -v
        Enables verbose output: all output from the invoked commands (e.g.
        dotnet, jq, etc.) to be sent to 'stdout' instead of '/dev/null'. It also
        enables the output from the script function trace().
        Initial value from \$VERBOSE or 'false'

    --trace | -x
        Sets the Bash trace option 'set -x' and enables the output from the
        functions 'trace' and 'dump_vars'.
        Initial value from \$TRACE_ENABLED or 'false'

    --short-run | -s
        A shortcut for '--define SHORT_RUN'. See below.
        Initial value from \$DEFINE.

    --force-new-baseline | -f
        When specified, a new baseline will be created even if a previous
        baseline already exists.
        Initial value from \$FORCE_NEW_BASELINE or 'false'

Options:
    --define | -d
        Defines one or more user-defined pre-processor symbols to be used when
        building the benchmark project, e.g. 'SHORT_RUN'. Which generates a
        shorter and faster, but less accurate benchmark run. You can specify
        this option multiple times to define multiple symbols.
        Initial value from \$DEFINE or ''

    --configuration | -c
        Specifies the build configuration to use ('Debug' or 'Release').
        Initial value from \$CONFIGURATION or 'Release'

    --artifacts | -a
        Specifies the directory where to create the benchmark artifacts:
        results, summaries, base lines, etc.
        Initial value from \$ARTIFACTS_DIR or '\$solution_dir/BmArtifacts'
        ($solution_dir/BmArtifacts)

    --max-regression-pct | -r
        Specifies the maximum acceptable regression percentage (0-100) when
        comparing to a previous, base-line benchmark results.
        Initial value from \$MAX_REGRESSION_PCT or 10

EOF
}
