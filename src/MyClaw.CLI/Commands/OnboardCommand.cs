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

你是 myclaw，一个个人 AI 助手。

你可以使用文件操作、网络搜索和命令执行等工具。
使用它们来帮助用户完成任务。

## 指南
- 简洁且有帮助
- 需要时主动使用工具
- 通过写入记忆来记住用户告诉你的信息
- 检查记忆上下文以获取之前存储的信息
";

    private const string DefaultSoulMd = @"# 灵魂

你是一个能干的个人助手，可以帮助处理日常任务、
研究、编程和一般问题。

你的性格：
- 直接且高效
- 需要时技术性强，可能时简单明了
- 主动使用工具来获取真实答案
";

    public OnboardCommand() : base("onboard", "初始化配置和工作区")
    {
        this.SetHandler(() =>
        {
            var cfgDir = ConfigurationLoader.ConfigDir;
            var cfgPath = ConfigurationLoader.ConfigPath;

            Directory.CreateDirectory(cfgDir);

            // 如果配置文件不存在则创建默认配置
            if (!File.Exists(cfgPath))
            {
                var defaultConfig = MyClawConfiguration.Default();
                ConfigurationLoader.Save(defaultConfig);
                AnsiConsole.MarkupLine($"[green]已创建配置: {cfgPath}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]配置已存在: {cfgPath}[/]");
            }

            // 加载配置以获取工作区
            var cfg = ConfigurationLoader.Load();
            var ws = cfg.Agent.Workspace;

            // 创建目录
            Directory.CreateDirectory(Path.Combine(ws, "memory"));
            AnsiConsole.MarkupLine($"[green]已创建工作区: {ws}[/]");

            var skillsDir = string.IsNullOrEmpty(cfg.Skills.Dir) 
                ? Path.Combine(ws, "skills") 
                : cfg.Skills.Dir;
            Directory.CreateDirectory(skillsDir);
            AnsiConsole.MarkupLine($"[green]已创建技能目录: {skillsDir}[/]");

            // 创建默认文件
            WriteIfNotExists(Path.Combine(ws, "AGENTS.md"), DefaultAgentsMd);
            WriteIfNotExists(Path.Combine(ws, "SOUL.md"), DefaultSoulMd);
            WriteIfNotExists(Path.Combine(ws, "memory", "MEMORY.md"), "");
            WriteIfNotExists(Path.Combine(ws, "HEARTBEAT.md"), "");

            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[blue]下一步:[/]");
            AnsiConsole.MarkupLine($"  1. 编辑 [yellow]{cfgPath}[/] 设置你的 API 密钥");
            AnsiConsole.MarkupLine("  2. 或设置 MYCLAW_API_KEY 环境变量");
            AnsiConsole.MarkupLine($"  3. 在 [yellow]{skillsDir}[/] 下添加技能（可选）");
            AnsiConsole.MarkupLine("  4. 运行 '[yellow]myclaw agent -m \"你好\"[/]' 测试");
        });
    }

    private static void WriteIfNotExists(string path, string content)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
                AnsiConsole.MarkupLine($"[green]已创建: {path}[/]");
        }
    }
}
