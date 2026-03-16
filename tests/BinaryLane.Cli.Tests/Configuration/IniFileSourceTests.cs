using BinaryLane.Cli.Infrastructure.Configuration;
using FluentAssertions;

namespace BinaryLane.Cli.Tests.Configuration;

public class IniFileSourceTests : IDisposable
{
    private readonly string _tempDir;

    public IniFileSourceTests()
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
    public void Read_ExistingConfig_ReturnsValues()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        File.WriteAllText(configPath, """
            [bl]
            api-token = my-secret-token
            api-url = https://api.binarylane.com.au
            """);

        var source = new IniFileSource(configPath);
        source.Get("api-token").Should().Be("my-secret-token");
        source.Get("api-url").Should().Be("https://api.binarylane.com.au");
    }

    [Fact]
    public void Read_DifferentSection_ReturnsNull()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        File.WriteAllText(configPath, """
            [staging]
            api-token = staging-token
            """);

        var source = new IniFileSource(configPath);
        // Default section is "bl", staging token should not be visible
        source.Get("api-token").Should().BeNull();
    }

    [Fact]
    public void Read_SwitchSection_ReturnsCorrectValues()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        File.WriteAllText(configPath, """
            [bl]
            api-token = prod-token

            [staging]
            api-token = staging-token
            """);

        var source = new IniFileSource(configPath);
        source.Get("api-token").Should().Be("prod-token");

        source.SectionName = "staging";
        source.Get("api-token").Should().Be("staging-token");
    }

    [Fact]
    public void Save_CreatesFile_WithCorrectFormat()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        var source = new IniFileSource(configPath);

        source.Save(new Dictionary<string, string?>
        {
            ["api-token"] = "new-token",
            ["api-url"] = "https://custom.api.com",
        });

        var content = File.ReadAllText(configPath);
        content.Should().Contain("[bl]");
        content.Should().Contain("api-token = new-token");
        content.Should().Contain("api-url = https://custom.api.com");
    }

    [Fact]
    public void Save_RemovesNullValues()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        File.WriteAllText(configPath, """
            [bl]
            api-token = old-token
            api-url = https://old.api.com
            """);

        var source = new IniFileSource(configPath);
        source.Save(new Dictionary<string, string?>
        {
            ["api-token"] = "new-token",
            ["api-url"] = null, // should be removed
        });

        var reloaded = new IniFileSource(configPath);
        reloaded.Get("api-token").Should().Be("new-token");
        reloaded.Get("api-url").Should().BeNull();
    }

    [Fact]
    public void Read_NonexistentFile_ReturnsNull()
    {
        var source = new IniFileSource(Path.Combine(_tempDir, "nonexistent.ini"));
        source.Get("api-token").Should().BeNull();
    }

    [Fact]
    public void Read_CommentsIgnored()
    {
        var configPath = Path.Combine(_tempDir, "config.ini");
        File.WriteAllText(configPath, """
            # This is a comment
            ; This is also a comment
            [bl]
            api-token = token-value
            """);

        var source = new IniFileSource(configPath);
        source.Get("api-token").Should().Be("token-value");
    }
}
