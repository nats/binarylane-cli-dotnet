using System.CommandLine;
using BinaryLane.Cli.Infrastructure.Configuration;

namespace BinaryLane.Cli.Commands;

/// <summary>
/// Binds global options from ParseResult to CommandContext.
/// </summary>
public static class ContextBinder
{
    public static CommandContext Bind(ParseResult parseResult)
    {
        var config = new AppConfiguration();

        var context = parseResult.GetValue(GlobalOptions.Context);
        var apiToken = parseResult.GetValue(GlobalOptions.ApiToken);
        var apiUrl = parseResult.GetValue(GlobalOptions.ApiUrl);
        var apiDev = parseResult.GetValue(GlobalOptions.ApiDevelopment);

        if (context != null) config.CommandLine.Set(OptionName.Context, context);
        if (apiToken != null) config.CommandLine.Set(OptionName.ApiToken, apiToken);
        if (apiUrl != null) config.CommandLine.Set(OptionName.ApiUrl, apiUrl);
        if (apiDev) config.CommandLine.Set(OptionName.ApiDevelopment, "true");

        var outputFormat = parseResult.GetValue(GlobalOptions.Output);
        var noHeader = parseResult.GetValue(GlobalOptions.NoHeader);
        var curl = parseResult.GetValue(GlobalOptions.CurlOption);
        var format = parseResult.GetValue(GlobalOptions.Format);
        var singleColumn = parseResult.GetValue(GlobalOptions.SingleColumn);

        return new CommandContext(config)
        {
            OutputFormat = outputFormat,
            NoHeader = noHeader,
            Curl = curl,
            FormatFields = format,
            SingleColumn = singleColumn,
        };
    }
}
