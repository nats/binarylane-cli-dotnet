using System.Text.RegularExpressions;
using FluentAssertions;

namespace BinaryLane.Cli.IntegrationTests;

/// <summary>
/// Runs both bl (Python) and blnet (.NET) with --curl and verifies equivalent HTTP requests.
/// </summary>
public partial class CurlComparisonTests
{
    public static TheoryData<string[]> CommandsWithCurl => new()
    {
        new[] { "server", "list" },
        new[] { "domain", "list" },
        new[] { "region", "list" },
        new[] { "size", "list" },
        new[] { "ssh-key", "list" },
        new[] { "image", "list" },
        new[] { "vpc", "list" },
        new[] { "software", "list" },
        new[] { "action", "list" },
        new[] { "account", "get" },
        new[] { "account", "balance" },
        new[] { "domain", "create", "--name", "test.example.com" },
        new[] { "server", "get", "12345" },
        new[] { "domain", "get", "example.com" },
        new[] { "ssh-key", "get", "12345" },
        new[] { "server", "delete", "12345" },
    };

    [Theory]
    [MemberData(nameof(CommandsWithCurl))]
    public async Task CurlOutput_ShouldBeEquivalent(string[] commandArgs)
    {
        string[] args = [.. commandArgs, "--curl"];

        var blpy = await CliRunner.BlpyAsync(args);
        var blnet = await CliRunner.BlnetAsync(args);

        var blpyCurl = NormalizeCurl(blpy.Stdout);
        var blnetCurl = NormalizeCurl(blnet.Stdout);

        blpyCurl.Should().StartWith("curl", $"blpy '{string.Join(' ', args)}' should produce curl output");
        blnetCurl.Should().StartWith("curl", $"blnet '{string.Join(' ', args)}' should produce curl output");

        var blpyParsed = ParseCurl(blpyCurl);
        var blnetParsed = ParseCurl(blnetCurl);

        blnetParsed.Method.Should().Be(blpyParsed.Method, "HTTP method should match");
        blnetParsed.Url.Should().Be(blpyParsed.Url, "URL should match");
        blnetParsed.AuthHeader.Should().Be(blpyParsed.AuthHeader, "Authorization header should match");

        if (blpyParsed.Data != null)
        {
            blnetParsed.Data.Should().NotBeNull("both should have request body");
            NormalizeJson(blnetParsed.Data!).Should().Be(NormalizeJson(blpyParsed.Data!), "request body should match");
        }
    }

    private static string NormalizeCurl(string curl) =>
        MultipleSpaces().Replace(curl.Replace("\\\n", " ").Trim(), " ");

    private static CurlParsed ParseCurl(string curl)
    {
        var method = Regex.Match(curl, @"--request\s+(\w+)") is { Success: true } m ? m.Groups[1].Value : "GET";
        var url = Regex.Match(curl, """curl\s+(?:--request\s+\w+\s+)?['"]?([^'\s]+)['"]?""") is { Success: true } u ? u.Groups[1].Value : "";
        var auth = Regex.Match(curl, @"--header\s+'(Authorization:[^']+)'") is { Success: true } a ? a.Groups[1].Value : "";
        var data = Regex.Match(curl, @"--data\s+'([^']+)'") is { Success: true } d ? d.Groups[1].Value : null;
        return new(method, url, auth, data);
    }

    private static string NormalizeJson(string json)
    {
        try
        {
            var el = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            return System.Text.Json.JsonSerializer.Serialize(el);
        }
        catch { return json; }
    }

    private record CurlParsed(string Method, string Url, string AuthHeader, string? Data);

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpaces();
}
