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
}
