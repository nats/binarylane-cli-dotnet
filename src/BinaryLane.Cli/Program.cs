using System.CommandLine;
using System.CommandLine.Help;
using BinaryLane.Cli.Commands;

var rootCommand = new RootCommand("BinaryLane command-line interface");

// Add global options
GlobalOptions.AddToCommand(rootCommand);

// Customize help to match blpy format
for (int i = 0; i < rootCommand.Options.Count; i++)
{
    if (rootCommand.Options[i] is HelpOption helpOption)
    {
        helpOption.Action = new BlpyHelpAction((HelpAction)helpOption.Action!);
        break;
    }
}

// Add manual commands
rootCommand.Subcommands.Add(ConfigureCommand.Create());
rootCommand.Subcommands.Add(VersionCommand.Create());

// Add generated API commands
BinaryLane.Cli.Generated.Commands.CommandRegistry.Register(rootCommand);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
