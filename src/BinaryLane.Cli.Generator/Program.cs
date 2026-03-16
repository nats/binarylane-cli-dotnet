using BinaryLane.Cli.Generator;

// Parse arguments
var specPath = args.Length > 0 ? args[0] : FindSpecFile();
var outputDir = args.Length > 1 ? args[1] : FindOutputDir();

if (specPath == null || !File.Exists(specPath))
{
    Console.Error.WriteLine($"OpenAPI spec not found. Usage: dotnet run [spec.json] [output-dir]");
    Console.Error.WriteLine($"Looked for: {specPath ?? "openapi.json in repo root"}");
    return 1;
}

Console.WriteLine($"Parsing OpenAPI spec: {specPath}");
var operations = OpenApiParser.Parse(specPath);
Console.WriteLine($"Found {operations.Count} CLI operations");

// Ensure output directory exists
var commandsDir = Path.Combine(outputDir, "Commands");
Directory.CreateDirectory(commandsDir);

// Generate command files
int generated = 0;
foreach (var op in operations)
{
    var code = CommandEmitter.Emit(op);
    var filePath = Path.Combine(commandsDir, $"{op.ClassName}.generated.cs");
    File.WriteAllText(filePath, code);
    generated++;
}

Console.WriteLine($"Generated {generated} command files in {commandsDir}");

// Generate command registry
var registryCode = RegistryEmitter.Emit(operations);
var registryPath = Path.Combine(commandsDir, "CommandRegistry.generated.cs");
File.WriteAllText(registryPath, registryCode);
Console.WriteLine($"Generated command registry: {registryPath}");

Console.WriteLine("Code generation complete.");
return 0;

static string? FindSpecFile()
{
    // Look for openapi.json in repo root or current directory
    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "openapi.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "openapi.json"),
        "openapi.json",
    };

    foreach (var candidate in candidates)
    {
        var full = Path.GetFullPath(candidate);
        if (File.Exists(full)) return full;
    }
    return null;
}

static string FindOutputDir()
{
    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BinaryLane.Cli", "Generated"),
        Path.Combine(Directory.GetCurrentDirectory(), "src", "BinaryLane.Cli", "Generated"),
    };

    foreach (var candidate in candidates)
    {
        var full = Path.GetFullPath(candidate);
        var parentDir = Path.GetDirectoryName(full)!;
        if (Directory.Exists(parentDir)) return full;
    }
    return Path.Combine(Directory.GetCurrentDirectory(), "src", "BinaryLane.Cli", "Generated");
}
