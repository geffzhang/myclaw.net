using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using MyClaw.Agent;
using MyClaw.Core.Configuration;
using MyClaw.Memory;
using MyClaw.Skills;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Agent 命令 - 单次消息或 REPL 模式
/// </summary>
public class AgentCommand : Command
{
    public AgentCommand() : base("agent", "Run agent in single message or REPL mode")
    {
        var messageOption = new Option<string?>(
            aliases: new[] { "-m", "--message" },
            description: "Single message to send to agent");

        AddOption(messageOption);

        this.SetHandler(async (string? message) =>
        {
            var cfg = ConfigurationLoader.Load();
            
            if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]API key not set. Run 'myclaw onboard' or set MYCLAW_API_KEY / ANTHROPIC_API_KEY[/]");
                return;
            }

            // 初始化依赖
            var memoryStore = new MemoryStore(cfg.Agent.Workspace);
            var skillManager = new SkillManager(cfg.Agent.Workspace);
            skillManager.LoadSkills();

            // 创建 Agent
            var model = ModelFactory.Create(cfg.Provider);
            var agent = new MyClawAgent(cfg, model, memoryStore, skillManager);

            if (!string.IsNullOrEmpty(message))
            {
                // Single message mode
                await RunSingleMessageAsync(agent, message);
            }
            else
            {
                // REPL mode
                await RunReplAsync(agent);
            }
        }, messageOption);
    }

    private async Task RunSingleMessageAsync(MyClawAgent agent, string message)
    {
        string response = "";
        await AnsiConsole.Status()
            .StartAsync("Thinking...", async ctx =>
            {
                response = await agent.ChatAsync(message);
            });

        AnsiConsole.MarkupLine($"[green]Assistant:[/] {response}");
    }

    private async Task RunReplAsync(MyClawAgent agent)
    {
        AnsiConsole.MarkupLine("[blue]myclaw agent (type 'exit' to quit)[/]");
        
        while (true)
        {
            var input = AnsiConsole.Ask<string?>("> ");
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.ToLower() is "exit" or "quit")
                break;

            string response = "";
            await AnsiConsole.Status()
                .StartAsync("Thinking...", async ctx =>
                {
                    response = await agent.ChatAsync(input);
                });

            AnsiConsole.MarkupLine($"[green]Assistant:[/] {response}");
        }
    }
}
