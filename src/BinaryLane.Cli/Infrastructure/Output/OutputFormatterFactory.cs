namespace BinaryLane.Cli.Infrastructure.Output;

public static class OutputFormatterFactory
{
    public static IOutputFormatter Create(OutputFormat format) => format switch
    {
        OutputFormat.Table => new TableFormatter(),
        OutputFormat.Json => new JsonFormatter(),
        OutputFormat.Plain => new PlainFormatter(),
        OutputFormat.Tsv => new TsvFormatter(),
        OutputFormat.None => new NoneFormatter(),
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };
}
