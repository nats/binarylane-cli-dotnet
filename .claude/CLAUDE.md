## Learn Directive

User input beginning with `learn:` is a directive to update this file to include the information provided. `learn: session` is a directive to update this file with new learnings made during the session.

## Development Process

- All functional changes via red/green/refactor: write a failing test first, make it pass, then refactor.
- When fixing something, consider whether the fix needs to be lifted up and applied more broadly across the codebase.

# BinaryLane CLI (.NET)

.NET 10 drop-in replacement for the Python BinaryLane CLI (`bl`).

## Project Structure

```
src/BinaryLane.Cli/              # Main CLI executable (assembly: bl)
src/BinaryLane.Cli.Generator/    # Code generator (reads openapi.json → Generated/)
tests/BinaryLane.Cli.Tests/      # Unit tests (xUnit)
tests/BinaryLane.Cli.IntegrationTests/  # Integration tests (runs bl + blnet side-by-side)
openapi.json                     # OpenAPI spec from api.binarylane.com.au
```

## Build & Test

```bash
make generate    # Regenerate code from openapi.json
make build       # Build all projects
make publish     # Publish self-contained single-file executable
make install     # Publish + copy to ~/.local/bin/blnet
make test        # Install + run all tests (unit + integration)
make clean       # Clean build outputs + remove installed binary
```

Integration tests require both `bl` (Python) and `blnet` (.NET) in `~/.local/bin/`.
Run `make install` before `dotnet test` on integration tests.

## Code Generation

115 command classes are generated from `openapi.json` into `src/BinaryLane.Cli/Generated/Commands/`.
Generated files are checked in. To regenerate after OpenAPI spec changes:

```bash
dotnet run --project src/BinaryLane.Cli.Generator -- openapi.json src/BinaryLane.Cli/Generated
```

The generator reads these OpenAPI extensions:
- `x-cli-command` — command tree path (e.g. "server action power-on")
- `x-cli-format` — sort order for default list display fields
- `#Fragment` in path — discriminator type value for polymorphic action bodies

Not yet consumed (lookup/autocomplete feature):
- `x-cli-entity-lookup`, `x-cli-entity-list`, `x-cli-entity-ref`

## Key Design Decisions

- **System.CommandLine 2.0** for CLI parsing (SetAction/ParseResult API)
- **Spectre.Console** for table output formatting
- **System.Text.Json source generators** (`CliJsonContext`) for trim-safe serialization
- Published binary is self-contained, single-file, trimmed (~18MB)
- No model classes generated — commands work directly with `Dictionary<string, object?>` bodies and `JsonElement` responses

## Compatibility with Python CLI (blpy)

Must match exactly:
- Command tree and option names (driven by `x-cli-command`)
- Environment variables: `BL_API_TOKEN`, `BL_API_URL`, `BL_API_DEVELOPMENT`, `BL_CONTEXT`
- Config file: `~/.config/binarylane/config.ini` (Python configparser INI format)
- `--curl` output (validated by integration tests)
- `--output` formats: table, json, tsv, plain, none
- `--format` field selection
- Default field display order (`x-cli-format`)
- Nested object flattening: display_name > full_name > name > slug > id
