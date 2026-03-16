using BinaryLane.Cli.Infrastructure.Configuration;
using FluentAssertions;

namespace BinaryLane.Cli.Tests.Configuration;

public class AppConfigurationTests : IDisposable
{
    private readonly string _tempDir;

    public AppConfigurationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Defaults_ApiUrl_IsSet()
    {
        var config = CreateConfig();
        config.ApiUrl.Should().Be("https://api.binarylane.com.au");
    }

    [Fact]
    public void Defaults_ApiToken_IsUnconfigured()
    {
        var config = CreateConfig();
        config.ApiToken.Should().Be(AppConfiguration.UnconfiguredToken);
    }

    [Fact]
    public void Defaults_Context_IsBl()
    {
        var config = CreateConfig();
        config.Context.Should().Be("bl");
    }

    [Fact]
    public void CommandLine_OverridesFileAndDefaults()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        File.WriteAllText(configPath, """
            [bl]
            api-token = file-token
            """);

        var config = CreateConfig(configPath);
        config.CommandLine.Set(OptionName.ApiToken, "cli-token");

        config.ApiToken.Should().Be("cli-token");
    }

    [Fact]
    public void FileSource_OverridesDefaults()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        File.WriteAllText(configPath, """
            [bl]
            api-token = file-token
            api-url = https://custom.api.com
            """);

        var config = CreateConfig(configPath);
        config.ApiToken.Should().Be("file-token");
        config.ApiUrl.Should().Be("https://custom.api.com");
    }

    [Fact]
    public void ContextSwitching_ReadsCorrectSection()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        File.WriteAllText(configPath, """
            [bl]
            api-token = prod-token

            [staging]
            api-token = staging-token
            """);

        var config = CreateConfig(configPath);
        config.CommandLine.Set(OptionName.Context, "staging");
        config.ResolveContext();

        config.ApiToken.Should().Be("staging-token");
    }

    [Fact]
    public void ApiDevelopment_DefaultsFalse()
    {
        var config = CreateConfig();
        config.ApiDevelopment.Should().BeFalse();
    }

    private AppConfiguration CreateConfig(string? configPath = null)
    {
        var fileSource = new IniFileSource(configPath ?? Path.Combine(_tempDir, "empty.ini"));
        return new AppConfiguration(fileSource);
    }
}
