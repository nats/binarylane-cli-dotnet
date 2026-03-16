namespace BinaryLane.Cli.Infrastructure.Configuration;

public sealed class EnvironmentSource : IConfigSource
{
    private const string Prefix = "BL_";
    private readonly Dictionary<string, string> _values;

    public EnvironmentSource()
    {
        _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Environment.GetEnvironmentVariables())
        {
            if (entry is System.Collections.DictionaryEntry de &&
                de.Key is string key &&
                de.Value is string value &&
                key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                var name = key[Prefix.Length..].ToLowerInvariant().Replace('_', '-');
                _values[name] = value;
            }
        }
    }

    public string? Get(string name) => _values.GetValueOrDefault(name);
}
