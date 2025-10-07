# Copilot Instructions

## Code Organization Principles

### Language Separation
- **Minimize mixing of different programming languages**: Keep languages separated as much as possible to improve maintainability, readability, and tooling support
- **Examples of what to avoid**:
  - Embedding large bash scripts within YAML workflows
  - Mixing SQL queries directly in C# string literals
  - Inline JavaScript within HTML templates
- **Preferred approach**:
  - Extract complex logic into dedicated script files
  - Use separate files for different languages (e.g., `.sh` for bash, `.sql` for SQL)
  - Reference external scripts from configuration files rather than embedding them

### Script Organization
- Keep shell scripts in the `scripts/bash/` directory
- Follow the established three-file pattern for complex scripts:
  - **Main script** (e.g., `script-name.sh`): Contains core business logic only
  - **Usage file** (e.g., `script-name.usage.sh`): Contains help text and documentation  
  - **Utils file** (e.g., `script-name.utils.sh`): Contains argument parsing and utility functions
- Use the common utility functions from `_common.sh`
- Maintain consistent error handling and validation patterns
- Keep common parameters and switches centralized in `_common.sh` via `$common_switches`

### Script Pattern Benefits
- **Maintainability**: Core logic remains uncluttered by long help texts
- **Side-by-side editing**: Usage and argument handling can be compared easily
- **Consistency**: Common parameters documented once, used everywhere
- **Testability**: Each component can be tested independently

### Workflow Organization
- Keep GitHub Actions workflows focused on orchestration
- Delegate complex logic to dedicated scripts
- Use environment variables to pass data between YAML and scripts
- Validate inputs at the script level, not in YAML
