namespace BinaryLane.Cli.Infrastructure.Output;

public interface IOutputFormatter
{
    void PrintObject(IReadOnlyDictionary<string, string?> nameValues, bool noHeader);
    void PrintTable(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows, bool noHeader);
}
