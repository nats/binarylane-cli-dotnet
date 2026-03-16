namespace BinaryLane.Cli.Infrastructure.Configuration;

public interface IConfigSource
{
    string? Get(string name);
}
