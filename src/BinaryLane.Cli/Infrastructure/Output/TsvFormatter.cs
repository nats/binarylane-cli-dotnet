namespace BinaryLane.Cli.Infrastructure.Output;

public sealed class TsvFormatter : IOutputFormatter
{
    public void PrintObject(IReadOnlyDictionary<string, string?> nameValues, bool noHeader)
    {
        foreach (var (key, value) in nameValues)
        {
            Console.WriteLine($"{key}\t{value}");
        }
    }

    public void PrintTable(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows, bool noHeader)
    {
        if (!noHeader)
        {
            Console.WriteLine(string.Join('\t', columns));
        }
        foreach (var row in rows)
        {
            Console.WriteLine(string.Join('\t', row.Select(v => v ?? "")));
        }
    }
}
