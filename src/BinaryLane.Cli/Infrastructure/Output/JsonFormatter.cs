using System.Text.Json;
using BinaryLane.Cli.Infrastructure.Json;

namespace BinaryLane.Cli.Infrastructure.Output;

public sealed class JsonFormatter : IOutputFormatter
{
    public void PrintObject(IReadOnlyDictionary<string, string?> nameValues, bool noHeader)
    {
        var dict = new Dictionary<string, string?>(nameValues);
        var json = JsonSerializer.Serialize(dict, CliJsonPrettyContext.Default.DictionaryStringString);
        Console.WriteLine(json);
    }

    public void PrintTable(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows, bool noHeader)
    {
        var items = rows.Select(row =>
            columns.Zip(row, (col, val) => new { col, val })
                .ToDictionary(x => x.col, x => (object?)x.val))
            .ToList();

        var json = JsonSerializer.Serialize(items, CliJsonPrettyContext.Default.ListDictionaryStringObject);
        Console.WriteLine(json);
    }
}
