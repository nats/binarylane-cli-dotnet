using BinaryLane.Cli.Infrastructure.Configuration;
using BinaryLane.Cli.Infrastructure.Http;
using BinaryLane.Cli.Infrastructure.Output;

namespace BinaryLane.Cli.Commands;

/// <summary>
/// Shared context available to all commands during execution.
/// </summary>
public sealed class CommandContext : IDisposable
{
    public AppConfiguration Config { get; }
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Table;
    public bool NoHeader { get; set; }
    public bool Curl { get; set; }
    public string? FormatFields { get; set; }
    public bool SingleColumn { get; set; }

    private BinaryLaneHttpClient? _httpClient;

    public CommandContext(AppConfiguration config)
    {
        Config = config;
    }

    public BinaryLaneHttpClient GetHttpClient()
    {
        if (_httpClient == null)
        {
            Config.ResolveContext();
            _httpClient = new BinaryLaneHttpClient(Config);
            if (Curl) _httpClient.CurlGenerator.Enabled = true;
        }
        return _httpClient;
    }

    public IOutputFormatter GetFormatter()
    {
        return OutputFormatterFactory.Create(OutputFormat);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
