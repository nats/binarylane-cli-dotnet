# blnet - Program Requirements

## Overview

blnet is a .NET drop-in replacement for blpy (the Python BinaryLane CLI). It wraps
the BinaryLane cloud API and must be CLI-compatible with blpy so users can switch
without changing scripts or workflows.

## Functional Requirements

### 1. Command Tree

Must match blpy's command tree exactly. Commands are derived from the `x-cli-command`
extension in the OpenAPI spec at `https://api.binarylane.com.au/reference/openapi.json`.

Examples: `server list`, `server action power-on`, `domain record create`,
`account invoice list`, `vpc members`, etc. (~115 commands total).

Manual (non-generated) commands: `configure`, `version`.

### 2. Code Generation

A prebuild code generator reads `openapi.json` and produces C# source files:
- One command class per API operation
- A command registry that wires the command tree

Must consume these OpenAPI extensions:
- `x-cli-command` — command name path (e.g. "server action power-on")
- `x-cli-format` — integer sort order for default list display fields
- URL fragment `#PowerOn` — discriminator type value for polymorphic action bodies

Not yet required (future): `x-cli-entity-lookup`, `x-cli-entity-list`,
`x-cli-entity-ref` (name→ID resolution/autocomplete).

### 3. CLI Compatibility

#### Global Options (recursive, available on all commands)
- `--context`, `-c` — authentication context name
- `--api-token`, `-t` — API bearer token
- `--api-url` — override API base URL
- `--api-development` — disable SSL verification
- `--output {table|json|plain|tsv|none}` — output format (default: table)
- `--no-header` — suppress column headers
- `--curl` — print equivalent curl command instead of executing
- `--format <fields>` — comma-separated field list for list commands
- `-1`, `--single-column` — one item per line

#### Positional Arguments
Path parameters (e.g. `server_id`) are positional arguments, matching blpy.

#### Body Parameters
Request body properties become `--option-name` flags. Required properties
use `--option.IsRequired = true`.

#### Discriminator Auto-Set
For polymorphic server action endpoints (URL contains `#Fragment`), the `type`
body field is excluded from CLI options and automatically set to the snake_case
equivalent of the fragment (e.g. `#PowerOn` → `type: "power_on"`).

### 4. Environment Variables

Must support the same env vars as blpy, with `BL_` prefix:
- `BL_API_TOKEN`
- `BL_API_URL`
- `BL_API_DEVELOPMENT`
- `BL_CONTEXT`

### 5. Configuration File

Read/write `~/.config/binarylane/config.ini` (or platform equivalent).
Must be round-trip compatible with Python's `configparser` format:
- Section headers: `[section_name]`
- Key-value: `key = value`
- Comments: `#`, `;`
- Default section: `[bl]`

#### Priority (highest to lowest)
1. Command-line arguments
2. Environment variables
3. Config file (section selected by `--context`)
4. Defaults (`api-url` = `https://api.binarylane.com.au`, `context` = `bl`)

### 6. Output Formatting

#### Table (default)
Spectre.Console tables with borders (rounded if TTY, simple if piped).

#### JSON
Raw API response, pretty-printed.

#### TSV / Plain
Tab-separated or space-separated columns.

#### None
Suppress output.

#### Nested Object Flattening
Objects displayed as scalar values using priority:
`display_name` > `full_name` > `name` > `slug` > `id`.

Networks objects: first v4 + first v6 `ip_address`, newline-separated.

Fallback: `<object>`.

#### Default Fields
Determined by `x-cli-format` sort order on response item schema properties.
Fallback: required primitive fields.

### 7. --curl Option

Intercept HTTP request before sending, format as shell curl command, print to
stdout, exit with code 0. Must produce functionally equivalent output to blpy
(same URL, method, auth header, request body).

### 8. Pagination

List endpoints with `page`/`per_page` parameters are auto-paginated.
All pages accumulated into a single response before formatting.
Detect next page via `response.links.pages.next`.
Handle `"links": null` gracefully.

### 9. Error Handling

- HTTP 401 → "Please run 'bl configure'" message, exit code 3
- HTTP 4xx/5xx → extract `message` or `detail` from response body, exit code 4
- Unconfigured token → use `"unconfigured"` as bearer token (allows public endpoints)

### 10. Publishing

- NativeAOT with static musl linking (fully static binary, no runtime dependencies)
- Static OpenSSL linked via musl-gcc cross-compilation
- Single binary, ~14MB, ~5ms startup

## Non-Functional Requirements

- Follow .NET conventions (DI-ready, separated concerns, proper project structure)
- Use NuGet packages: System.CommandLine, Spectre.Console, xUnit, FluentAssertions
- System.Text.Json source generators for trim/AOT-safe serialization
- Generated code checked into source control
- Unit tests for configuration, output formatting, response flattening
- Integration tests that run both blpy and blnet and compare outputs
