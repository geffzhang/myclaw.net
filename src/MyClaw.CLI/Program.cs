using System;
using System.CommandLine;
using System.Threading.Tasks;
using MyClaw.CLI.Commands;
using MyClaw.Core.Configuration;
using Spectre.Console;

namespace MyClaw.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var rootCommand = new RootCommand("myclaw - Personal AI assistant");

            // Add subcommands
            rootCommand.AddCommand(new AgentCommand());
            rootCommand.AddCommand(new GatewayCommand());
            rootCommand.AddCommand(new OnboardCommand());
            rootCommand.AddCommand(new StatusCommand());
            rootCommand.AddCommand(new SkillsCommand());

            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
