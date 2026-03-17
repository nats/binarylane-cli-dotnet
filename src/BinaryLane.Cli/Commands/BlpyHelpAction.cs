using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace BinaryLane.Cli.Commands;

/// <summary>
/// Custom help action that formats help output like blpy (Python BinaryLane CLI).
/// Groups options into: Options (global), Arguments (required), Parameters (optional).
/// Falls back to default help for group commands (with subcommands).
/// </summary>
internal sealed class BlpyHelpAction : SynchronousCommandLineAction
{
    private readonly HelpAction _defaultHelp;

    public BlpyHelpAction(HelpAction defaultHelp) => _defaultHelp = defaultHelp;

    public override int Invoke(ParseResult parseResult)
    {
        var command = parseResult.CommandResult.Command;

        // For group commands (with subcommands), use default help
        if (command.Subcommands.Count > 0)
            return _defaultHelp.Invoke(parseResult);

        WriteBlpyHelp(command, parseResult);
        return 0;
    }

    private static void WriteBlpyHelp(Command command, ParseResult parseResult)
    {
        var writer = Console.Out;

        // Categorize command-specific options
        var positionalArgs = command.Arguments.ToList();
        var commandOptions = command.Options
            .Where(o => !o.Hidden && o is not HelpOption && !IsGlobalOption(o))
            .ToList();
        var requiredOpts = commandOptions.Where(o => o.Required).ToList();
        var optionalOpts = commandOptions.Where(o => !o.Required).ToList();
        bool isListCommand = command.Name == "list";

        // Collect all labels to compute column width
        var allLabels = new List<string>();
        foreach (var (label, _) in GetGlobalEntries(isListCommand))
            allLabels.Add(label);
        foreach (var arg in positionalArgs)
            allLabels.Add(FormatArgDisplayName(arg));
        foreach (var opt in requiredOpts)
            allLabels.Add(FormatOptionLabel(opt));
        foreach (var opt in optionalOpts)
            allLabels.Add(FormatOptionLabel(opt));

        // Match Python argparse: max label width + 2 indent + 2 padding, capped at 24
        int helpPos = allLabels.Count > 0
            ? Math.Min(allLabels.Max(l => l.Length) + 4, 24)
            : 24;

        // Usage line
        var commandPath = GetCommandPath(parseResult);
        writer.Write($"usage: bl {commandPath} [OPTIONS]");
        foreach (var arg in positionalArgs)
            writer.Write($" {FormatArgDisplayName(arg)}");
        foreach (var opt in requiredOpts)
            writer.Write($" {FormatOptionLabel(opt)}");
        if (optionalOpts.Count > 0)
            writer.Write(" [PARAMETERS]");
        writer.WriteLine();
        writer.WriteLine();

        // Description
        if (!string.IsNullOrEmpty(command.Description))
        {
            writer.WriteLine(command.Description);
            writer.WriteLine();
        }

        // Options section (global)
        writer.WriteLine("Options:");
        foreach (var (label, description) in GetGlobalEntries(isListCommand))
            WriteEntry(writer, label, description, helpPos);

        // Arguments section (positional args + required options)
        if (positionalArgs.Count > 0 || requiredOpts.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Arguments:");
            foreach (var arg in positionalArgs)
                WriteEntry(writer, FormatArgDisplayName(arg), arg.Description ?? "", helpPos);
            foreach (var opt in requiredOpts)
                WriteEntry(writer, FormatOptionLabel(opt), opt.Description ?? "", helpPos);
        }

        // Parameters section (optional command-specific)
        if (optionalOpts.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Parameters:");
            foreach (var opt in optionalOpts)
                WriteEntry(writer, FormatOptionLabel(opt), opt.Description ?? "", helpPos);
        }
    }

    private static IEnumerable<(string label, string description)> GetGlobalEntries(bool isListCommand)
    {
        yield return ("--help", "Display command options and descriptions");
        yield return ("--context NAME", "Name of authentication context to use (default: \"bl\")");
        yield return ("--api-token VALUE", "API token to use with BinaryLane API");
        yield return ("--curl", "Display API request as a 'curl' command-line");
        yield return ("--no-header", "Display columns without field labels");
        yield return ("--output OUTPUT", "Desired output format [plain, table, tsv, json, none] (default: \"table\")");
        if (isListCommand)
        {
            yield return ("--format FIELD,...", "Comma-separated list of fields to display. Wildcards are supported: e.g. --format \"*\" will display all fields.");
            yield return ("-1, --single-column", "List one id per line.");
        }
    }

    private static bool IsGlobalOption(Option option) =>
        option == GlobalOptions.Context ||
        option == GlobalOptions.ApiToken ||
        option == GlobalOptions.ApiUrl ||
        option == GlobalOptions.ApiDevelopment ||
        option == GlobalOptions.Output ||
        option == GlobalOptions.NoHeader ||
        option == GlobalOptions.CurlOption ||
        option == GlobalOptions.Format ||
        option == GlobalOptions.SingleColumn;

    private static string GetCommandPath(ParseResult parseResult)
    {
        var parts = new List<string>();
        SymbolResult? current = parseResult.CommandResult;
        while (current is CommandResult cmdResult)
        {
            if (cmdResult.Command is not RootCommand)
                parts.Insert(0, cmdResult.Command.Name);
            current = cmdResult.Parent;
        }
        return string.Join(" ", parts);
    }

    private static string FormatArgDisplayName(Argument arg)
    {
        var name = arg.Name;
        // Strip _id suffix to match blpy's entity name display (server_id → SERVER)
        if (name.EndsWith("_id"))
            name = name[..^3];
        return name.Replace('_', '-').ToUpperInvariant();
    }

    private static string FormatOptionLabel(Option option)
    {
        var name = option.Name; // includes -- prefix
        if (option.ValueType == typeof(bool))
            return name;
        var valueName = name.TrimStart('-').Replace('-', '_').ToUpperInvariant();
        return $"{name} {valueName}";
    }

    private static void WriteEntry(TextWriter writer, string label, string description, int helpPos)
    {
        var indented = $"  {label}";
        if (indented.Length >= helpPos)
        {
            writer.WriteLine(indented);
            WriteWrapped(writer, description, helpPos);
        }
        else
        {
            writer.Write(indented.PadRight(helpPos));
            WriteWrapped(writer, description, helpPos, firstLineWritten: true);
        }
    }

    private static void WriteWrapped(TextWriter writer, string text, int indent, bool firstLineWritten = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            writer.WriteLine();
            return;
        }

        const int maxWidth = 80;
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int col = firstLineWritten ? indent : 0;
        bool firstWord = true;

        if (!firstLineWritten)
        {
            writer.Write(new string(' ', indent));
            col = indent;
        }

        foreach (var word in words)
        {
            if (!firstWord && col + 1 + word.Length > maxWidth)
            {
                writer.WriteLine();
                writer.Write(new string(' ', indent));
                col = indent;
                firstWord = true;
            }

            if (!firstWord)
            {
                writer.Write(' ');
                col++;
            }

            writer.Write(word);
            col += word.Length;
            firstWord = false;
        }

        writer.WriteLine();
    }
}
