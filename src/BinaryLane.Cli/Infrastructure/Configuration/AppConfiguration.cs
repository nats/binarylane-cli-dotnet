namespace BinaryLane.Cli.Infrastructure.Configuration;

/// <summary>
/// Manages configuration from multiple sources with priority:
/// 1. Command-line arguments (highest)
/// 2. Environment variables (BL_*)
/// 3. Config file section
/// 4. Defaults (lowest)
/// </summary>
public sealed class AppConfiguration
{
    public const string UnconfiguredToken = "unconfigured";

    private readonly List<IConfigSource> _sources = [];
    private readonly IniFileSource _fileSource;

    public CommandLineSource CommandLine { get; } = new();

    public AppConfiguration() : this(new IniFileSource()) { }

    public AppConfiguration(IniFileSource fileSource)
    {
        _fileSource = fileSource;

        // Order: command-line, environment, file, defaults (first match wins)
        _sources.Add(CommandLine);
        _sources.Add(new EnvironmentSource());
        _sources.Add(_fileSource);
        _sources.Add(new DefaultSource());
    }

    public string? Get(string name)
    {
        foreach (var source in _sources)
        {
            var value = source.Get(name);
            if (value != null) return value;
        }
        return null;
    }

    public string GetRequired(string name)
    {
        return Get(name) ?? throw new InvalidOperationException($"Configuration option '{name}' is required but not set.");
    }

    public string ApiUrl => GetRequired(OptionName.ApiUrl);

    public string ApiToken => Get(OptionName.ApiToken) ?? UnconfiguredToken;

    public bool ApiDevelopment
    {
        get
        {
            var value = Get(OptionName.ApiDevelopment);
            if (value == null) return false;
            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value == "1"
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }

    public string Context
    {
        get
        {
            var context = GetRequired(OptionName.Context);
            _fileSource.SectionName = context;
            return context;
        }
    }

    /// <summary>
    /// Resolve the context first, so file source reads from the correct section,
    /// then subsequent reads for api-token etc. use the right section.
    /// </summary>
    public void ResolveContext()
    {
        // Reading Context triggers the section name update on the file source.
        _ = Context;
    }

    public void Save(string? apiToken = null, string? apiUrl = null, bool? apiDevelopment = null)
    {
        var options = new Dictionary<string, string?>
        {
            [OptionName.ApiToken] = apiToken,
            [OptionName.ApiUrl] = apiUrl,
            [OptionName.ApiDevelopment] = apiDevelopment?.ToString().ToLowerInvariant(),
        };
        _fileSource.Save(options);
    }
}
