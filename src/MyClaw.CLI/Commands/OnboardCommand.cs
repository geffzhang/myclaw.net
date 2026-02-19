using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using MyClaw.Core.Configuration;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Onboard 命令 - 初始化配置和工作区
/// </summary>
public class OnboardCommand : Command
{
    private const string DefaultAgentsMd = @"# myclaw Agent

You are myclaw, a personal AI assistant.

You have access to tools for file operations, web search, and command execution.
Use them to help the user accomplish tasks.

## Guidelines
- Be concise and helpful
- Use tools proactively when needed
- Remember information the user tells you by writing to memory
- Check your memory context for previously stored information
";

    private const string DefaultSoulMd = @"# Soul

You are a capable personal assistant that helps with daily tasks,
research, coding, and general questions.

Your personality:
- Direct and efficient
- Technical when needed, simple when possible
- Proactive about using tools to get real answers
";

    public OnboardCommand() : base("onboard", "Initialize config and workspace")
    {
        this.SetHandler(() =>
        {
            var cfgDir = ConfigurationLoader.ConfigDir;
            var cfgPath = ConfigurationLoader.ConfigPath;

            Directory.CreateDirectory(cfgDir);

            // Create default config if not exists
            if (!File.Exists(cfgPath))
            {
                var defaultConfig = MyClawConfiguration.Default();
                ConfigurationLoader.Save(defaultConfig);
                AnsiConsole.MarkupLine($"[green]Created config: {cfgPath}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Config already exists: {cfgPath}[/]");
            }

            // Load config to get workspace
            var cfg = ConfigurationLoader.Load();
            var ws = cfg.Agent.Workspace;

            // Create directories
            Directory.CreateDirectory(Path.Combine(ws, "memory"));
            AnsiConsole.MarkupLine($"[green]Created workspace: {ws}[/]");

            var skillsDir = string.IsNullOrEmpty(cfg.Skills.Dir) 
                ? Path.Combine(ws, "skills") 
                : cfg.Skills.Dir;
            Directory.CreateDirectory(skillsDir);
            AnsiConsole.MarkupLine($"[green]Created skills dir: {skillsDir}[/]");

            // Create default files
            WriteIfNotExists(Path.Combine(ws, "AGENTS.md"), DefaultAgentsMd);
            WriteIfNotExists(Path.Combine(ws, "SOUL.md"), DefaultSoulMd);
            WriteIfNotExists(Path.Combine(ws, "memory", "MEMORY.md"), "");
            WriteIfNotExists(Path.Combine(ws, "HEARTBEAT.md"), "");

            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[blue]Next steps:[/]");
            AnsiConsole.MarkupLine($"  1. Edit [yellow]{cfgPath}[/] to set your API key");
            AnsiConsole.MarkupLine("  2. Or set MYCLAW_API_KEY environment variable");
            AnsiConsole.MarkupLine($"  3. Add skills under [yellow]{skillsDir}[/] (optional)");
            AnsiConsole.MarkupLine("  4. Run '[yellow]myclaw agent -m \"Hello\"[/]' to test");
        });
    }

    private static void WriteIfNotExists(string path, string content)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
            AnsiConsole.MarkupLine($"[green]Created: {path}[/]");
        }
    }
}
