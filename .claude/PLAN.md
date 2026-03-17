# blnet - Implementation Plan

Reproducible step-by-step plan for building the .NET BinaryLane CLI from scratch.

## Prerequisites

- .NET 10 SDK
- musl-dev, musl-tools (for static AOT build)
- cmake (for NativeAOT OpenSSL shim)
- Python blpy CLI installed at `~/.local/bin/bl` (for integration tests)

## Phase 1: Research blpy

1. Read blpy's project structure, entry point, command dispatch
2. Read the OpenAPI spec — identify `x-cli-command`, `x-cli-format`, `x-cli-entity-*`
3. Read blpy's config handling (`sources.py`, `options.py`) — INI format, env vars, priority
4. Read blpy's output formatting (`formatter.py`) — `_flatten_dict` priority, networks handling
5. Read blpy's `--curl` implementation (`httpx_wrapper.py`)
6. Read blpy's command templates — understand generated code patterns
7. Catalog all 115 operations with: CLI command, HTTP method, path, params, body, pagination

## Phase 2: Solution Setup

1. Create solution: `dotnet new sln`
2. Create projects:
   - `src/BinaryLane.Cli` — main CLI (OutputType Exe, AssemblyName bl)
   - `src/BinaryLane.Cli.Generator` — code generator
   - `tests/BinaryLane.Cli.Tests` — unit tests (xUnit + FluentAssertions)
   - `tests/BinaryLane.Cli.IntegrationTests` — integration tests
3. Add NuGet packages:
   - CLI: System.CommandLine, Spectre.Console, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Http
   - Generator: (none — uses System.Text.Json from framework)
   - Tests: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, FluentAssertions
4. Create `Directory.Build.props` (net10.0, nullable, implicit usings, warnings as errors)
5. Download `openapi.json` to repo root

## Phase 3: Core Infrastructure

### Configuration (`Infrastructure/Configuration/`)
1. `IConfigSource` — interface with `string? Get(string name)`
2. `DefaultSource` — api-url = `https://api.binarylane.com.au`, context = `bl`
3. `EnvironmentSource` — reads `BL_*` env vars, strips prefix, converts `_` to `-`
4. `CommandLineSource` — mutable dict, populated from parsed CLI options
5. `IniFileSource` — reads/writes INI files compatible with Python configparser
6. `AppConfiguration` — chains sources in priority order, exposes typed properties
7. `OptionName` — constants: `api-url`, `api-token`, `api-development`, `context`

### HTTP (`Infrastructure/Http/`)
1. `CurlGenerator` — DelegatingHandler that intercepts requests, prints curl, returns dummy
2. `BinaryLaneHttpClient` — creates HttpClient with auth header, base URL, SSL config

### Output (`Infrastructure/Output/`)
1. `OutputFormat` enum — Table, Json, Plain, Tsv, None
2. `IOutputFormatter` — PrintObject(nameValues) and PrintTable(columns, rows)
3. `TableFormatter` — Spectre.Console tables
4. `JsonFormatter` — source-generated JSON serialization
5. `PlainFormatter` — space-separated
6. `TsvFormatter` — tab-separated
7. `NoneFormatter` — no-op
8. `OutputFormatterFactory` — creates formatter from enum
9. `ResponseFlattener` — ExtractPrimary (unwrap response wrappers), FlattenList, FlattenObject, nested object flattening with display_name/full_name/name/slug/id priority

### JSON (`Infrastructure/Json/`)
1. `CliJsonContext` — source-generated JsonSerializerContext for Dictionary, JsonElement, primitives
2. `CliJsonPrettyContext` — same but with WriteIndented = true

### Commands (`Commands/`)
1. `GlobalOptions` — static Option<T> instances, added to RootCommand with Recursive = true
2. `CommandContext` — holds AppConfiguration, output settings, creates HttpClient
3. `ContextBinder` — reads GlobalOptions from ParseResult into CommandContext
4. `ApiCommandBase` — ExecuteApiCallAsync (HTTP + error handling + formatting), ExecutePaginatedAsync
5. `ConfigureCommand` — interactive token setup
6. `VersionCommand` — prints assembly version
7. `Program.cs` — create RootCommand, add global options, add commands, parse and invoke

## Phase 4: Code Generator

### Data Model (`OperationInfo.cs`)
- CliCommand, HttpMethod, Path, Summary
- PathParameters, QueryParameters, BodyProperties
- IsPaginated, DefaultFields, FieldDescriptions
- IsActionResponse, HasActionsLink, DiscriminatorValue

### Parser (`OpenApiParser.cs`)
1. Parse `openapi.json` with System.Text.Json
2. For each path+method with `x-cli-command`:
   - Extract path params, query params (excluding page/per_page)
   - Resolve request body schema (handle `$ref`, `allOf`)
   - Extract body properties (skip `type` discriminator for polymorphic)
   - Find response list item schema, read `x-cli-format` for default fields
   - Detect URL fragment → compute discriminator value (PascalCase → snake_case)
3. Handle C# keyword conflicts (prefix with `@`)

### Command Emitter (`CommandEmitter.cs`)
For each operation, generate a static class with `Create()` method:
1. Create `Command` with name and description (sentence-cased via regex)
2. Add `Argument<string>` for path params (positional)
3. Add `Option<T>` for query params and body properties
4. `SetAction(async (parseResult, ct) => { ... })`:
   - Bind global options via ContextBinder
   - Read argument/option values from ParseResult
   - Build URL with path param interpolation (strip URL fragment)
   - Build query string from query params
   - Build body Dictionary with auto-set discriminator type
   - Serialize body via `CliJsonContext` (not reflection-based PostAsJsonAsync)
   - Call `ApiCommandBase.ExecuteApiCallAsync` with format callback for list commands

### Registry Emitter (`RegistryEmitter.cs`)
1. Collect all unique group paths (non-leaf command segments)
2. Create `Command` for each group with descriptive text
3. Wire group hierarchy via `Subcommands.Add()`
4. Wire leaf commands to their parent groups

### Entry Point (`Program.cs`)
1. Parse args: spec path and output directory
2. Run OpenApiParser
3. Run CommandEmitter for each operation
4. Run RegistryEmitter

## Phase 5: Generate and Build

1. `dotnet run --project src/BinaryLane.Cli.Generator -- openapi.json src/BinaryLane.Cli/Generated`
2. `dotnet build` — fix any compilation errors
3. Iterate: fix C# keyword escaping, API compatibility (System.CommandLine 2.0 uses
   SetAction/Options.Add/Arguments.Add/Subcommands.Add, not SetHandler/AddOption etc.)

## Phase 6: Publishing

### Self-Contained Single-File (initial)
- PublishSingleFile, SelfContained, PublishTrimmed, TrimMode=partial
- ~18MB, ~142ms startup (JIT runtime bundled)

### NativeAOT with Static Musl (final)
1. Set in csproj: PublishAot, StaticExecutable, StaticOpenSslLinking, RuntimeIdentifier=linux-musl-x64
2. Build static OpenSSL with musl-gcc:
   - Download openssl-3.0.13 source
   - Configure: `CC=musl-gcc CFLAGS="-idirafter /usr/include -idirafter /usr/include/x86_64-linux-gnu" ./Configure linux-x86_64 no-shared no-async no-tests`
   - Build with `CNF_CFLAGS` to pass kernel header paths through
   - Install to `vendor/musl-ssl/`
3. Create `vendor/musl-gcc-wrapper` — strips `--target=` flags (clang-only, breaks gcc)
   and adds `-L` path to musl OpenSSL libs
4. Publish: `OPENSSL_ROOT_DIR=vendor/musl-ssl dotnet publish -p:CppCompilerAndLinker=vendor/musl-gcc-wrapper`
5. Result: ~14MB fully static PIE binary, ~5ms startup

### Makefile targets
- `make openssl` — build static OpenSSL with musl (cached, only rebuilt if missing)
- `make publish` — depends on openssl, runs dotnet publish with musl-gcc-wrapper
- `make install` — publish + copy to ~/.local/bin/blnet
- `make test` — install + dotnet test
- `make generate` — regenerate from openapi.json

## Phase 7: Tests

### Unit Tests (`BinaryLane.Cli.Tests`)
- `IniFileSourceTests` — read sections, switch context, save/roundtrip, comments, missing file
- `AppConfigurationTests` — defaults, priority chain, context switching
- `ResponseFlattenerTests` — ExtractPrimary unwrapping, FlattenList field selection, bool→Yes/No, FlattenObject, GetAvailableFields

### Integration Tests (`BinaryLane.Cli.IntegrationTests`)
- `CliRunner` — shared helper that runs `bl`/`blnet` from `~/.local/bin/`
- `CurlComparisonTests` — run both CLIs with `--curl`, parse curl output, compare method/URL/auth/body for 16 commands
- `ServerActionCurlTests` — verify discriminator auto-set for 5 action commands
- `OutputComparisonTests` — run both CLIs with `--output tsv`, compare stdout for list commands with default and custom `--format`

## Key Gotchas Encountered

1. **System.CommandLine 2.0 API** — completely different from beta. `SetAction` not `SetHandler`, `Options.Add` not `AddOption`, `ParseResult.GetValue` not `GetValueForOption`, no `InvocationContext`
2. **C# keywords as identifiers** — `private`, `type` etc. from OpenAPI property names need `@` prefix
3. **allOf in OpenAPI** — body schemas use `allOf` with `$ref`, not direct `$ref`
4. **Pagination with null links** — API returns `"links": null` not missing `links`, must check `ValueKind == Object` before traversing
5. **ExtractPrimary** — combined pagination response has only the data property (no meta/links), must unwrap single-property objects when the value is an array
6. **PublishTrimmed + JSON** — `PostAsJsonAsync`/`JsonSerializer.Serialize` need reflection. Use source-generated `CliJsonContext` and manual `StringContent` serialization
7. **NativeAOT musl** — needs musl-built static OpenSSL. System headers via `-idirafter` to avoid glibc conflicts. `--target=` flag must be stripped for gcc (clang-only). `-lssl -lcrypto` need `-L` to find musl-built libs
