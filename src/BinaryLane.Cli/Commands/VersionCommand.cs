using System.CommandLine;
using System.Reflection;

namespace BinaryLane.Cli.Commands;

public static class VersionCommand
{
    public static Command Create()
    {
        var command = new Command("version", "Show the current version");
        command.SetAction(_ =>
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            Console.WriteLine($"bl {version}");
        });
        return command;
    }
}
