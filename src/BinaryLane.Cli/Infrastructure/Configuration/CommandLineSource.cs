namespace BinaryLane.Cli.Infrastructure.Configuration;

public sealed class CommandLineSource : IConfigSource
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string name, string value) => _values[name] = value;

    public string? Get(string name) => _values.GetValueOrDefault(name);
}
