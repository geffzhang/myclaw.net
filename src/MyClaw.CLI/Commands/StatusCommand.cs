using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using MyClaw.Core.Configuration;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Status 命令 - 显示 myclaw 状态
/// </summary>
public class StatusCommand : Command
{
    public StatusCommand() : base("status", "Show myclaw status")
    {
        this.SetHandler(() =>
        {
            MyClawConfiguration cfg;
            try
            {
                cfg = ConfigurationLoader.Load();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Config: error ({ex.Message})[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Config", ConfigurationLoader.ConfigPath);
            table.AddRow("Workspace", cfg.Agent.Workspace);
            table.AddRow("Model", cfg.Agent.Model);
            table.AddRow("Provider", string.IsNullOrEmpty(cfg.Provider.Type) ? "anthropic (default)" : cfg.Provider.Type);

            // API Key (masked) with source
            var (apiKeyDisplay, keySource) = GetApiKeyDisplay(cfg);
            table.AddRow("API Key", apiKeyDisplay);
            if (!string.IsNullOrEmpty(keySource))
            {
                table.AddRow("Key Source", $"[blue]{keySource}[/]");
            }

            // Show Base URL if set
            if (!string.IsNullOrEmpty(cfg.Provider.BaseUrl))
            {
                table.AddRow("Base URL", cfg.Provider.BaseUrl);
            }

            table.AddRow("Telegram", cfg.Channels.Telegram.Enabled ? "[green]enabled[/]" : "disabled");
            table.AddRow("Feishu", cfg.Channels.Feishu.Enabled ? "[green]enabled[/]" : "disabled");
            table.AddRow("WeCom", cfg.Channels.WeCom.Enabled ? "[green]enabled[/]" : "disabled");
            table.AddRow("Uno", cfg.Channels.Uno.Enabled ? $"[green]enabled[/] ({cfg.Channels.Uno.Mode})" : "disabled");
            table.AddRow("Skills", cfg.Skills.Enabled ? $"[green]enabled[/] (dir: {GetSkillsDir(cfg)})" : "disabled");

            // Check workspace
            if (!Directory.Exists(cfg.Agent.Workspace))
            {
                table.AddRow("Workspace", "[red]not found (run 'myclaw onboard')[/]");
            }
            else
            {
                var memoryPath = Path.Combine(cfg.Agent.Workspace, "memory", "MEMORY.md");
                if (File.Exists(memoryPath))
                {
                    var content = File.ReadAllText(memoryPath);
                    table.AddRow("Memory", $"{content.Length} bytes");
                }
                else
                {
                    table.AddRow("Memory", "empty");
                }
            }

            AnsiConsole.Write(table);
        });
    }

    private static string GetSkillsDir(MyClawConfiguration cfg)
    {
        return string.IsNullOrEmpty(cfg.Skills.Dir)
            ? Path.Combine(cfg.Agent.Workspace, "skills")
            : cfg.Skills.Dir;
    }

    private static (string display, string source) GetApiKeyDisplay(MyClawConfiguration cfg)
    {
        if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
        {
            return ("[red]not set[/]", "");
        }

        // Detect source from environment variables
        string source;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
            source = "env: OPENAI_API_KEY";
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")))
            source = "env: DEEPSEEK_API_KEY";
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            source = "env: ANTHROPIC_API_KEY";
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_API_KEY")))
            source = "env: MYCLAW_API_KEY";
        else
            source = "config file";

        // Mask the key
        string display;
        if (cfg.Provider.ApiKey.Length > 8)
        {
            display = cfg.Provider.ApiKey[..4] + "..." + cfg.Provider.ApiKey[^4..];
        }
        else
        {
            display = "set";
        }

        return (display, source);
    }
}
