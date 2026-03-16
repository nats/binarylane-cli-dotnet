namespace BinaryLane.Cli.Infrastructure.Output;

public sealed class NoneFormatter : IOutputFormatter
{
    public void PrintObject(IReadOnlyDictionary<string, string?> nameValues, bool noHeader) { }
    public void PrintTable(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows, bool noHeader) { }
}
