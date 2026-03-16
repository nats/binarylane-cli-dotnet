using System.CommandLine;
using BinaryLane.Cli.Commands;

var rootCommand = new RootCommand("BinaryLane command-line interface");

// Add global options
GlobalOptions.AddToCommand(rootCommand);

// Add manual commands
rootCommand.Subcommands.Add(ConfigureCommand.Create());
rootCommand.Subcommands.Add(VersionCommand.Create());

// Add generated API commands
BinaryLane.Cli.Generated.Commands.CommandRegistry.Register(rootCommand);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
