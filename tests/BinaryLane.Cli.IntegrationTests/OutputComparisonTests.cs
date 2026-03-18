using System.Text.Json;
using FluentAssertions;

namespace BinaryLane.Cli.IntegrationTests;

/// <summary>
/// Runs both bl and blnet without --curl and compares actual output.
/// </summary>
public class OutputComparisonTests
{
    public static TheoryData<string[]> ListCommands => new()
    {
        new[] { "vpc", "list" },
        new[] { "server", "list" },
        new[] { "size", "list", "--format", "size_type" },
    };

    [Theory]
    [MemberData(nameof(ListCommands))]
    public async Task ListOutput_ShouldMatch(string[] commandArgs)
    {
        string[] args = [.. commandArgs, "--output", "tsv"];

        var blpy = await CliRunner.BlpyAsync(args);
        var blnet = await CliRunner.BlnetAsync(args);

        blpy.ExitCode.Should().Be(0, $"blpy failed: {blpy.Stderr}");
        blnet.ExitCode.Should().Be(0, $"blnet failed: {blnet.Stderr}");

        blnet.Stdout.Should().Be(blpy.Stdout,
            $"blnet '{string.Join(' ', commandArgs)}' output should match blpy");
    }

    public static TheoryData<string[]> JsonListCommands => new()
    {
        new[] { "vpc", "list" },
        new[] { "ssh-key", "list" },
    };

    [Theory]
    [MemberData(nameof(JsonListCommands))]
    public async Task JsonListOutput_ShouldMatch(string[] commandArgs)
    {
        string[] args = [.. commandArgs, "--output", "json"];

        var blpy = await CliRunner.BlpyAsync(args);
        var blnet = await CliRunner.BlnetAsync(args);

        blpy.ExitCode.Should().Be(0, $"blpy failed: {blpy.Stderr}");
        blnet.ExitCode.Should().Be(0, $"blnet failed: {blnet.Stderr}");

        using var blpyDoc = JsonDocument.Parse(blpy.Stdout);
        using var blnetDoc = JsonDocument.Parse(blnet.Stdout);

        var blpyNorm = NormalizeJson(blpyDoc.RootElement);
        var blnetNorm = NormalizeJson(blnetDoc.RootElement);

        blnetNorm.Should().Be(blpyNorm,
            $"blnet '{string.Join(' ', commandArgs)} --output json' should match blpy");
    }

    /// <summary>
    /// Re-serializes JSON with sorted object keys so property order doesn't affect comparison.
    /// </summary>
    private static string NormalizeJson(JsonElement element)
    {
        return JsonSerializer.Serialize(SortProperties(element));
    }

    private static object? SortProperties(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToDictionary(p => p.Name, p => SortProperties(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(SortProperties).ToList(),
        _ => element
    };
}
