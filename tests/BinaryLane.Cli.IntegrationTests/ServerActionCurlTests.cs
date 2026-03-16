using System.Text.RegularExpressions;
using FluentAssertions;

namespace BinaryLane.Cli.IntegrationTests;

/// <summary>
/// Verifies server action commands auto-set the discriminator 'type' field
/// and match blpy curl output.
/// </summary>
public class ServerActionCurlTests
{
    public static TheoryData<string, string> ActionCommands => new()
    {
        { "power-on", "power_on" },
        { "power-off", "power_off" },
        { "shutdown", "shutdown" },
        { "reboot", "reboot" },
        { "ping", "ping" },
    };

    [Theory]
    [MemberData(nameof(ActionCommands))]
    public async Task ServerAction_ShouldAutoSetTypeDiscriminator(string action, string expectedType)
    {
        var args = new[] { "server", "action", action, "--curl", "12345" };

        var blpy = await CliRunner.BlpyAsync(args);
        var blnet = await CliRunner.BlnetAsync(args);

        blnet.ExitCode.Should().Be(0, $"blnet should succeed. stderr: {blnet.Stderr}");
        blnet.Stdout.Should().Contain($"\"type\":\"{expectedType}\"",
            $"blnet should auto-set type to {expectedType}");

        var blpyUrl = ExtractUrl(blpy.Stdout);
        var blnetUrl = ExtractUrl(blnet.Stdout);
        blnetUrl.Should().Be(blpyUrl);
    }

    private static string ExtractUrl(string curl)
    {
        var match = Regex.Match(curl, @"'?(https?://[^\s']+)'?");
        return match.Success ? match.Groups[1].Value.Split('#')[0] : "";
    }
}
