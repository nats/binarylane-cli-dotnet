using Spectre.Console;

namespace BinaryLane.Cli.Infrastructure.Output;

public sealed class TableFormatter : IOutputFormatter
{
    public void PrintObject(IReadOnlyDictionary<string, string?> nameValues, bool noHeader)
    {
        var table = CreateTable();
        table.AddColumn("Property");
        table.AddColumn("Value");
        table.HideHeaders();

        foreach (var (key, value) in nameValues)
        {
            table.AddRow(Markup.Escape(key), Markup.Escape(value ?? ""));
        }

        AnsiConsole.Write(table);
    }

    public void PrintTable(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows, bool noHeader)
    {
        var table = CreateTable();
        foreach (var col in columns)
        {
            table.AddColumn(Markup.Escape(col));
        }
        if (noHeader) table.HideHeaders();

        foreach (var row in rows)
        {
            table.AddRow(row.Select(v => Markup.Escape(v ?? "")).ToArray());
        }

        AnsiConsole.Write(table);
    }

    private static Spectre.Console.Table CreateTable()
    {
        var table = new Spectre.Console.Table();
        table.Border = Console.IsOutputRedirected ? TableBorder.Simple : TableBorder.Rounded;
        return table;
    }
}
