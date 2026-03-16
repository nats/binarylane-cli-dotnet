using System.CommandLine;
using BinaryLane.Cli.Infrastructure.Output;

namespace BinaryLane.Cli.Commands;

/// <summary>
/// Defines global options shared across all commands.
/// Added to root command with Recursive = true.
/// </summary>
public static class GlobalOptions
{
    public static readonly Option<string?> Context = new("--context")
    {
        Description = "Name of the authentication context to use",
    };

    public static readonly Option<string?> ApiToken = new("--api-token")
    {
        Description = "API bearer token",
    };

    public static readonly Option<string?> ApiUrl = new("--api-url")
    {
        Description = "Override the API base URL",
    };

    public static readonly Option<bool> ApiDevelopment = new("--api-development")
    {
        Description = "Disable SSL certificate verification",
    };

    public static readonly Option<OutputFormat> Output = new("--output")
    {
        Description = "Output format",
        DefaultValueFactory = _ => OutputFormat.Table,
    };

    public static readonly Option<bool> NoHeader = new("--no-header")
    {
        Description = "Suppress column headers in output",
    };

    public static readonly Option<bool> CurlOption = new("--curl")
    {
        Description = "Display the equivalent curl command instead of executing",
    };

    public static readonly Option<string?> Format = new("--format")
    {
        Description = "Comma-separated list of fields to display (list commands only)",
    };

    public static readonly Option<bool> SingleColumn = new("--single-column")
    {
        Description = "Display one item per line (list commands only)",
    };

    public static void AddToCommand(RootCommand root)
    {
        // Add aliases
        Context.Aliases.Add("-c");
        ApiToken.Aliases.Add("-t");
        SingleColumn.Aliases.Add("-1");

        // Mark all as recursive (global)
        Context.Recursive = true;
        ApiToken.Recursive = true;
        ApiUrl.Recursive = true;
        ApiDevelopment.Recursive = true;
        Output.Recursive = true;
        NoHeader.Recursive = true;
        CurlOption.Recursive = true;
        Format.Recursive = true;
        SingleColumn.Recursive = true;

        root.Options.Add(Context);
        root.Options.Add(ApiToken);
        root.Options.Add(ApiUrl);
        root.Options.Add(ApiDevelopment);
        root.Options.Add(Output);
        root.Options.Add(NoHeader);
        root.Options.Add(CurlOption);
        root.Options.Add(Format);
        root.Options.Add(SingleColumn);
    }
}
