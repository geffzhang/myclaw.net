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
    public AgentCommand() : base("agent", "在单消息或 REPL 模式下运行 Agent")
    {
        var messageOption = new Option<string?>(
            aliases: new[] { "-m", "--message" },
            description: "发送给 Agent 的单条消息");

        AddOption(messageOption);

        this.SetHandler(async (string? message) =>
        {
            var cfg = ConfigurationLoader.Load();
            
            if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]API 密钥未设置。请运行 'myclaw onboard' 或设置 MYCLAW_API_KEY / ANTHROPIC_API_KEY[/]");
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
                // 单消息模式
                await RunSingleMessageAsync(agent, message);
            }
            else
            {
                // REPL 模式
                await RunReplAsync(agent);
            }
        }, messageOption);
    }

    private async Task RunSingleMessageAsync(MyClawAgent agent, string message)
    {
        string response = "";
        await AnsiConsole.Status()
            .StartAsync("思考中...", async ctx =>
            {
                response = await agent.ChatAsync(message);
            });

            AnsiConsole.MarkupLine($"[green]助手:[/] {response}");
    }

    private async Task RunReplAsync(MyClawAgent agent)
    {
        AnsiConsole.MarkupLine("[blue]myclaw agent (输入 'exit' 退出)[/]");
        
        while (true)
        {
            var input = AnsiConsole.Ask<string?>("> ");
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.ToLower() is "exit" or "quit")
                break;

            string response = "";
            await AnsiConsole.Status()
                .StartAsync("思考中...", async ctx =>
                {
                    response = await agent.ChatAsync(input);
                });

        AnsiConsole.MarkupLine($"[green]助手:[/] {response}");
        }
    }
}
