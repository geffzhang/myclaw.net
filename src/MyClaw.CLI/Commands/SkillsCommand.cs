using System.CommandLine;
using System.IO;
using System.Linq;
using MyClaw.Core.Configuration;
using MyClaw.Skills;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Skills 命令 - 管理技能
/// </summary>
public class SkillsCommand : Command
{
    public SkillsCommand() : base("skills", "Inspect configured skills")
    {
        AddCommand(new SkillsListCommand());
        AddCommand(new SkillsInfoCommand());
        AddCommand(new SkillsCheckCommand());
    }
}

/// <summary>
/// Skills list 子命令
/// </summary>
public class SkillsListCommand : Command
{
    public SkillsListCommand() : base("list", "List loaded skills")
    {
        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "Output as JSON");

        AddOption(jsonOption);

        this.SetHandler((bool json) =>
        {
            var cfg = ConfigurationLoader.Load();
            var skillDir = SkillsCommandHelpers.GetSkillsDir(cfg);

            // 加载 Skills
            var manager = new SkillManager(skillDir);
            manager.LoadSkills();
            var skills = manager.LoadedSkills;

            if (json)
            {
                var skillsJson = skills.Select(s => new
                {
                    name = s.Name,
                    description = string.IsNullOrEmpty(s.Description) ? "(no description)" : s.Description,
                    keywords = s.Keywords
                });

                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    command = "skills.list",
                    ok = true,
                    enabled = cfg.Skills.Enabled,
                    dir = skillDir,
                    loaded = skills.Count,
                    skills = skillsJson
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                Console.WriteLine(result);
            }
            else
            {
                AnsiConsole.MarkupLine($"Skills: enabled={cfg.Skills.Enabled}, dir={skillDir}");

                if (!cfg.Skills.Enabled)
                {
                    AnsiConsole.MarkupLine("[yellow]Skills are disabled in config.[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"Loaded skills: {skills.Count}");

                if (skills.Count == 0)
                {
                    AnsiConsole.MarkupLine("No skills found.");
                }
                else
                {
                    foreach (var skill in skills)
                    {
                        var desc = string.IsNullOrEmpty(skill.Description) 
                            ? "(no description)" 
                            : skill.Description;
                        AnsiConsole.MarkupLine($"- [blue]{skill.Name}[/]: {desc}");
                    }
                }
            }
        }, jsonOption);
    }
}

/// <summary>
/// Skills info 子命令
/// </summary>
public class SkillsInfoCommand : Command
{
    public SkillsInfoCommand() : base("info", "Show skill details")
    {
        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "Output as JSON");

        var nameArgument = new Argument<string>("name", "Skill name");

        AddOption(jsonOption);
        AddArgument(nameArgument);

        this.SetHandler((string name, bool json) =>
        {
            var cfg = ConfigurationLoader.Load();
            
            if (!cfg.Skills.Enabled)
            {
                AnsiConsole.MarkupLine("[red]Skills are disabled in config[/]");
                return;
            }

            var skillDir = SkillsCommandHelpers.GetSkillsDir(cfg);
            var manager = new SkillManager(skillDir);
            manager.LoadSkills();

            var skill = manager.GetSkill(name);
            if (skill == null)
            {
                if (json)
                {
                    Console.WriteLine($"{{ \"schemaVersion\": 1, \"command\": \"skills.info\", \"ok\": false, \"error\": \"Skill not found: {name}\" }}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Skill not found: {name}[/]");
                }
                return;
            }

            if (json)
            {
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    command = "skills.info",
                    ok = true,
                    name = skill.Name,
                    description = skill.Description,
                    dir = skillDir,
                    keywords = skill.Keywords,
                    source = skill.SourcePath,
                    preview = string.Join("\n", skill.Content.Split('\n').Take(8))
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                Console.WriteLine(result);
            }
            else
            {
                AnsiConsole.MarkupLine($"[blue]Name:[/] {skill.Name}");
                
                var desc = string.IsNullOrEmpty(skill.Description) 
                    ? "(no description)" 
                    : skill.Description;
                AnsiConsole.MarkupLine($"[blue]Description:[/] {desc}");
                
                AnsiConsole.MarkupLine($"[blue]Skills dir:[/] {skillDir}");
                
                if (skill.Keywords.Count == 0)
                {
                    AnsiConsole.MarkupLine("[blue]Keywords:[/] (none)");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[blue]Keywords:[/] {string.Join(", ", skill.Keywords)}");
                }

                AnsiConsole.MarkupLine($"[blue]Source:[/] {skill.SourcePath}");
                
                var preview = string.Join("\n", skill.Content.Split('\n').Take(8));
                if (!string.IsNullOrEmpty(preview))
                {
                    AnsiConsole.MarkupLine("[blue]Prompt preview:[/]");
                    AnsiConsole.MarkupLine(preview);
                }
            }
        }, nameArgument, jsonOption);
    }
}

/// <summary>
/// Skills check 子命令
/// </summary>
public class SkillsCheckCommand : Command
{
    public SkillsCheckCommand() : base("check", "Check skills directory and loading status")
    {
        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "Output as JSON");

        AddOption(jsonOption);

        this.SetHandler((bool json) =>
        {
            var cfg = ConfigurationLoader.Load();
            var skillDir = SkillsCommandHelpers.GetSkillsDir(cfg);

            if (!json)
            {
                AnsiConsole.MarkupLine($"Skills: enabled={cfg.Skills.Enabled}, dir={skillDir}");
            }

            if (!cfg.Skills.Enabled)
            {
                if (json)
                {
                    Console.WriteLine(@"{ ""schemaVersion"": 1, ""command"": ""skills.check"", ""ok"": true, ""enabled"": false, ""result"": ""disabled"" }");
                }
                else
                {
                    AnsiConsole.MarkupLine("Result: [yellow]disabled[/]");
                }
                return;
            }

            var skillFolders = 0;
            var missingSkillMd = new List<string>();

            if (Directory.Exists(skillDir))
            {
                var dirs = Directory.GetDirectories(skillDir);
                skillFolders = dirs.Length;
                missingSkillMd = dirs.Where(d => !File.Exists(Path.Combine(d, "SKILL.md")))
                    .Select(Path.GetFileName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList()!;
            }

            // 加载统计
            var manager = new SkillManager(skillDir);
            manager.LoadSkills();
            var loaded = manager.LoadedSkills.Count;

            if (json)
            {
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    command = "skills.check",
                    ok = true,
                    enabled = cfg.Skills.Enabled,
                    dir = skillDir,
                    skillFolders,
                    loaded,
                    missingSkillMD = missingSkillMd,
                    result2 = "ok"
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                Console.WriteLine(result);
            }
            else
            {
                AnsiConsole.MarkupLine($"Skill folders: {skillFolders}");
                AnsiConsole.MarkupLine($"Loaded skills: {loaded}");
                if (missingSkillMd.Count > 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]Missing SKILL.md: {string.Join(", ", missingSkillMd)}[/]");
                }
                AnsiConsole.MarkupLine("Result: [green]ok[/]");
            }
        }, jsonOption);
    }
}

internal static class SkillsCommandHelpers
{
    public static string GetSkillsDir(MyClawConfiguration cfg)
    {
        return string.IsNullOrEmpty(cfg.Skills.Dir)
            ? Path.Combine(cfg.Agent.Workspace, "skills")
            : cfg.Skills.Dir;
    }
}
