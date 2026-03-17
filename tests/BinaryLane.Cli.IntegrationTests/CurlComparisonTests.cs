using System.Text.RegularExpressions;
using FluentAssertions;

namespace BinaryLane.Cli.IntegrationTests;

/// <summary>
/// Runs both bl (Python) and blnet (.NET) with --curl for all endpoints
/// and verifies they produce equivalent HTTP requests.
/// URL fragments (#PowerOn etc.) are stripped since they aren't sent over HTTP.
/// </summary>
public partial class CurlComparisonTests
{
    public static TheoryData<string[]> AllCommands => new()
    {
        // Account
        new[] { "account", "get" },
        new[] { "account", "balance" },
        new[] { "account", "invoice", "list" },
        new[] { "account", "invoice", "get", "12345" },
        new[] { "account", "invoice", "overdue" },

        // Action
        new[] { "action", "list" },
        new[] { "action", "get", "12345" },
        new[] { "action", "proceed", "12345", "--proceed" },

        // Domain
        new[] { "domain", "list" },
        new[] { "domain", "create", "--name", "test" },
        new[] { "domain", "get", "test.example.com" },
        new[] { "domain", "get", "12345" },
        new[] { "domain", "delete", "test.example.com" },
        new[] { "domain", "nameservers", "list" },
        new[] { "domain", "refresh-nameserver-cache", "--domain-names", "test" },

        // Domain Record
        new[] { "domain", "record", "list", "test.example.com" },
        new[] { "domain", "record", "list", "12345" },
        new[] { "domain", "record", "create", "test.example.com", "--type", "A", "--name", "test", "--data", "test" },
        new[] { "domain", "record", "get", "test.example.com", "12345" },
        new[] { "domain", "record", "update", "test.example.com", "12345" },
        new[] { "domain", "record", "delete", "test.example.com", "12345" },

        // Image
        new[] { "image", "list" },
        new[] { "image", "get", "ubuntu-22.04" },
        new[] { "image", "get", "12345" },
        new[] { "image", "update", "12345" },
        new[] { "image", "download", "12345" },

        // Load Balancer
        new[] { "load-balancer", "list" },
        new[] { "load-balancer", "create", "--name", "test" },
        new[] { "load-balancer", "availability" },
        new[] { "load-balancer", "get", "12345" },
        new[] { "load-balancer", "update", "12345", "--name", "test" },
        new[] { "load-balancer", "delete", "12345" },
        // TODO: implement +item list syntax for forwarding-rules
        // new[] { "load-balancer", "rule", "create", "12345", "--forwarding-rules", "test" },
        // new[] { "load-balancer", "rule", "delete", "12345", "--forwarding-rules", "test" },
        // TODO: blpy uses --servers, blnet uses --server-ids — option name compatibility
        // new[] { "load-balancer", "server", "create", "12345", "--servers", "12345" },
        // new[] { "load-balancer", "server", "delete", "12345", "--servers", "12345" },

        // Region
        new[] { "region", "list" },

        // SSH Key
        new[] { "ssh-key", "list" },
        new[] { "ssh-key", "create", "--public-key", "test", "--name", "test" },
        new[] { "ssh-key", "get", "12345" },
        new[] { "ssh-key", "get", "aa:bb:cc:dd" },
        new[] { "ssh-key", "update", "12345", "--name", "test" },
        new[] { "ssh-key", "delete", "12345" },

        // Size
        new[] { "size", "list" },

        // Software
        new[] { "software", "list" },
        new[] { "software", "get", "12345" },
        new[] { "software", "operating-system", "12345" },
        new[] { "software", "operating-system", "ubuntu" },

        // VPC
        new[] { "vpc", "list" },
        new[] { "vpc", "create", "--name", "test" },
        // TODO: implement +item list syntax for +route
        // new[] { "vpc", "create", "--name", "test",
        //         "+route", "--router", "10.240.1.1", "--destination", "192.168.0.0/24", "--description", "Subnet 1",
        //         "+route", "--router", "10.240.1.2", "--destination", "192.168.0.0/24", "--description", "Subnet 2" },
        new[] { "vpc", "get", "12345" },
        new[] { "vpc", "update", "12345", "--name", "test" },
        new[] { "vpc", "patch", "12345" },
        new[] { "vpc", "delete", "12345" },
        new[] { "vpc", "members", "12345" },

        // Server CRUD
        new[] { "server", "list" },
        new[] { "server", "create", "--size", "test", "--image", "1", "--region", "test" },
        new[] { "server", "create", "--size", "test", "--image", "ubuntu", "--region", "test" },
        new[] { "server", "get", "12345" },
        new[] { "server", "delete", "12345" },
        new[] { "server", "console", "12345" },
        new[] { "server", "user-data", "12345" },
        new[] { "server", "software", "12345" },
        new[] { "server", "alert", "list" },
        new[] { "server", "alert", "get", "12345" },

        // Server sub-resources
        new[] { "server", "data-usage", "list" },
        new[] { "server", "data-usage", "get", "12345" },
        new[] { "server", "metrics", "list", "12345" },
        new[] { "server", "metrics", "get", "12345" },
        new[] { "server", "action", "list", "12345" },
        new[] { "server", "action", "get", "12345", "12345" },
        new[] { "server", "firewall", "list", "12345" },
        new[] { "server", "feature", "list", "12345" },
        new[] { "server", "backup", "list", "12345" },
        new[] { "server", "backup", "upload", "12345", "--replacement-strategy", "none", "--url", "test" },
        new[] { "server", "kernel", "list", "12345" },
        new[] { "server", "snapshot", "list", "12345" },
        new[] { "server", "ipv6-ptr-ns", "list" },
        new[] { "server", "ipv6-ptr-ns", "update", "--reverse-nameservers", "test" },

        // Server Actions
        new[] { "server", "action", "add-disk", "12345", "--size-gigabytes", "1" },
        new[] { "server", "action", "attach-backup", "12345", "--image", "1" },
        new[] { "server", "action", "change-advanced-features", "12345" },
        // TODO: implement +item list syntax for firewall-rules
        // new[] { "server", "action", "change-advanced-firewall-rules", "12345", "--firewall-rules", "test" },
        new[] { "server", "action", "change-backup-schedule", "12345" },
        new[] { "server", "action", "change-ipv6", "12345", "--enabled" },
        new[] { "server", "action", "change-ipv6-reverse-nameservers", "12345", "--ipv6-reverse-nameservers", "test" },
        new[] { "server", "action", "change-kernel", "12345", "--kernel", "1" },
        new[] { "server", "action", "change-manage-offsite-backup-copies", "12345", "--manage-offsite-backup-copies" },
        new[] { "server", "action", "change-network", "12345" },
        new[] { "server", "action", "change-offsite-backup-location", "12345" },
        new[] { "server", "action", "change-partner", "12345" },
        new[] { "server", "action", "change-port-blocking", "12345", "--enabled" },
        new[] { "server", "action", "change-region", "12345", "--region", "test" },
        new[] { "server", "action", "change-reverse-name", "12345", "--ipv4-address", "test" },
        new[] { "server", "action", "change-separate-private-network-interface", "12345", "--enabled" },
        new[] { "server", "action", "change-source-and-destination-check", "12345", "--enabled" },
        // TODO: implement +item list syntax for threshold-alerts
        // new[] { "server", "action", "change-threshold-alerts", "12345", "--threshold-alerts", "test" },
        new[] { "server", "action", "change-vpc-ipv4", "12345", "--current-ipv4-address", "test", "--new-ipv4-address", "test" },
        new[] { "server", "action", "clone-using-backup", "12345", "--image-id", "1", "--target-server-id", "1" },
        new[] { "server", "action", "delete-disk", "12345", "--disk-id", "1" },
        new[] { "server", "action", "detach-backup", "12345" },
        new[] { "server", "action", "disable-backups", "12345" },
        new[] { "server", "action", "disable-selinux", "12345" },
        new[] { "server", "action", "enable-backups", "12345" },
        new[] { "server", "action", "enable-ipv6", "12345" },
        new[] { "server", "action", "is-running", "12345" },
        new[] { "server", "action", "password-reset", "12345" },
        new[] { "server", "action", "ping", "12345" },
        new[] { "server", "action", "power-cycle", "12345" },
        new[] { "server", "action", "power-off", "12345" },
        new[] { "server", "action", "power-on", "12345" },
        new[] { "server", "action", "reboot", "12345" },
        new[] { "server", "action", "rebuild", "12345" },
        new[] { "server", "action", "rebuild", "12345", "--image", "1" },
        new[] { "server", "action", "rebuild", "12345", "--image", "ubuntu" },
        new[] { "server", "action", "rename", "12345", "--name", "test" },
        // TODO: implement +item list syntax — blpy always sends change_licenses wrapper
        // new[] { "server", "action", "resize", "12345" },
        new[] { "server", "action", "resize-disk", "12345", "--disk-id", "1", "--size-gigabytes", "1" },
        new[] { "server", "action", "restore", "12345", "--image", "1" },
        new[] { "server", "action", "restore", "12345", "--image", "ubuntu" },
        new[] { "server", "action", "shutdown", "12345" },
        new[] { "server", "action", "take-backup", "12345", "--replacement-strategy", "none" },
        new[] { "server", "action", "uncancel", "12345" },
        new[] { "server", "action", "uptime", "12345" },

        // TODO: implement +item list syntax for +license
        // new[] { "server", "create", "--size", "std-min", "--image", "1", "--region", "syd",
        //         "--name", "test-server", "--ipv6", "--vpc", "12345", "--ssh-keys", "1", "2",
        //         "--daily-backups", "1", "--weekly-backups", "0", "--monthly-backups", "0",
        //         "--ipv4-addresses", "1", "--user-data", "#!/bin/bash", "--password", "secret123",
        //         "+license", "--software-id", "1", "--count", "1",
        //         "+license", "--software-id", "2", "--count", "3" },
    };

    [Theory]
    [MemberData(nameof(AllCommands))]
    public async Task CurlOutput_ShouldBeEquivalent(string[] commandArgs)
    {
        string[] args = [.. commandArgs, "--curl"];

        var blpy = await CliRunner.BlpyAsync(args);
        var blnet = await CliRunner.BlnetAsync(args);

        var label = string.Join(' ', commandArgs);

        blpy.ExitCode.Should().Be(0, $"blpy '{label} --curl' failed: {blpy.Stderr}");
        blnet.ExitCode.Should().Be(0, $"blnet '{label} --curl' failed: {blnet.Stderr}");

        var blpyCurl = NormalizeCurl(blpy.Stdout);
        var blnetCurl = NormalizeCurl(blnet.Stdout);

        blpyCurl.Should().StartWith("curl", $"blpy '{label} --curl' should produce curl output");
        blnetCurl.Should().StartWith("curl", $"blnet '{label} --curl' should produce curl output");

        var blpyParsed = ParseCurl(blpyCurl);
        var blnetParsed = ParseCurl(blnetCurl);

        blnetParsed.Method.Should().Be(blpyParsed.Method, $"[{label}] HTTP method");
        StripFragment(blnetParsed.Url).Should().Be(StripFragment(blpyParsed.Url), $"[{label}] URL");
        blnetParsed.AuthHeader.Should().Be(blpyParsed.AuthHeader, $"[{label}] Authorization header");

        if (blpyParsed.Data != null)
        {
            blnetParsed.Data.Should().NotBeNull($"[{label}] both should have request body");
            NormalizeJson(blnetParsed.Data!).Should().Be(NormalizeJson(blpyParsed.Data!), $"[{label}] request body");
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

    private static string StripFragment(string url)
    {
        var idx = url.IndexOf('#');
        return idx >= 0 ? url[..idx] : url;
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
