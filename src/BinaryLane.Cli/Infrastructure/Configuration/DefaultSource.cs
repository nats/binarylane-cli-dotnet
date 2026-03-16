namespace BinaryLane.Cli.Infrastructure.Configuration;

public sealed class DefaultSource : IConfigSource
{
    private static readonly Dictionary<string, string> Defaults = new()
    {
        [OptionName.ApiUrl] = "https://api.binarylane.com.au",
        [OptionName.Context] = "bl",
    };

    public string? Get(string name) => Defaults.GetValueOrDefault(name);
}
