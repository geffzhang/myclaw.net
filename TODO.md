

## ✅ 已完成

本修改清单已于 **2026-02-23** 完成：

- ✅ 更新 ModelFactory.cs - 使用 AgentScope.Core.ModelFactory
- ✅ 更新 SkillTool.cs - 完善工具架构
- ✅ 更新 AgentCommand.cs - 添加 CLI 参数支持
- ✅ 更新 MyClawAgent.cs - 添加 HEARTBEAT.md 支持
- ✅ 添加 Verbose 配置到 AgentConfig
- ✅ 添加 Model 配置到 ProviderConfig
- ✅ 添加单元测试 (254 个测试全部通过)

---

## 📋 MyClaw.NET 修改清单

以下是您需要在 **MyClaw.NET** 中进行的修改：

### 1. 更新 ModelFactory (`src/MyClaw.Agent/ModelFactory.cs`)

**推荐方案**：直接使用 AgentScope.NET 的 ModelFactory

```csharp
using AgentScope.Core;
using AgentScope.Core.Model;
using MyClaw.Core.Configuration;

namespace MyClaw.Agent;

public static class ModelFactory
{
    public static IModel Create(ProviderConfig config)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            throw new InvalidOperationException("API key is required");
        }

        return AgentScope.Core.ModelFactory.Create(
            provider: config.Type?.ToLowerInvariant() ?? "anthropic",
            modelName: config.Model ?? AgentScope.Core.ModelFactoryExtensions.GetDefaultModel(config.Type ?? "anthropic"),
            apiKey: config.ApiKey,
            baseUrl: config.BaseUrl
        );
    }
}
```

---

### 2. 更新 SkillTool (`src/MyClaw.Agent/SkillTool.cs`)

```csharp
using AgentScope.Core.Tool;
using MyClaw.Skills;

namespace MyClaw.Agent;

public class SkillTool : ToolBase
{
    private readonly Skill _skill;

    public SkillTool(Skill skill) : base(skill.Name, skill.Description)
    {
        _skill = skill;
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["intent"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "用户想要完成的任务意图，如 '计算', '搜索', '写作' 等"
                },
                ["query"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "用户的具体查询或需求"
                }
            },
            ["required"] = new List<string> { "intent", "query" }
        };
    }

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var intent = parameters.TryGetValue("intent", out var i) ? i?.ToString() : null;
        var query = parameters.TryGetValue("query", out var q) ? q?.ToString() : null;

        var systemPrompt = _skill.GetSystemPrompt();
        
        var result = $"""
            [Skill: {_skill.Name}]
            [Intent: {intent}]
            [Query: {query}]
            
            {systemPrompt}
            """;

        return Task.FromResult(ToolResult.Ok(result));
    }
}
```

---

### 3. 更新 AgentCommand (`src/MyClaw.CLI/Commands/AgentCommand.cs`)

添加更多 CLI 参数支持：

```csharp
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using MyClaw.Agent;
using MyClaw.Core.Configuration;
using MyClaw.Memory;
using MyClaw.Skills;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

public class AgentCommand : Command
{
    public AgentCommand() : base("agent", "在单消息或 REPL 模式下运行 Agent")
    {
        var messageOption = new Option<string?>(
            aliases: new[] { "-m", "--message" },
            description: "发送给 Agent 的单条消息");
            
        var modelOption = new Option<string>(
            aliases: new[] { "--model", "-M" },
            description: "指定使用的模型",
            getDefaultValue: () => "anthropic");
            
        var replOption = new Option<bool>(
            aliases: new[] { "--repl", "-r" },
            description: "强制使用 REPL 模式");

        AddOption(messageOption);
        AddOption(modelOption);
        AddOption(replOption);

        this.SetHandler(async (string? message, string model, bool repl) =>
        {
            var cfg = ConfigurationLoader.Load();
            
            // 使用指定的模型或默认
            if (!string.IsNullOrEmpty(model))
            {
                cfg.Provider.Type = model;
            }
            
            if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]API 密钥未设置。请运行 'myclaw onboard' 或设置 MYCLAW_API_KEY / ANTHROPIC_API_KEY[/]");
                return;
            }

            var memoryStore = new MemoryStore(cfg.Agent.Workspace);
            var skillManager = new SkillManager(cfg.Agent.Workspace);
            skillManager.LoadSkills();

            var modelInstance = ModelFactory.Create(cfg.Provider);
            var agent = new MyClawAgent(cfg, modelInstance, memoryStore, skillManager);

            if (!string.IsNullOrEmpty(message) && !repl)
            {
                await RunSingleMessageAsync(agent, message);
            }
            else
            {
                await RunReplAsync(agent);
            }
        }, messageOption, modelOption, replOption);
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
        AnsiConsole.MarkupLine("[blue]myclaw agent (输入 'exit' 或 '/quit' 退出)[/]");
        
        while (true)
        {
            var input = AnsiConsole.Ask<string?>("> ");
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.ToLower() is "exit" or "quit" or "/quit")
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
```

---

### 4. 更新 MyClawAgent (`src/MyClaw.Agent/MyClawAgent.cs`)

添加 HEARTBEAT.md 支持和更多上下文：

```csharp
using System.Reactive.Linq;
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using MyClaw.Core.Configuration;
using MyClaw.Memory;
using MyClaw.Skills;

namespace MyClaw.Agent;

public class MyClawAgent
{
    private readonly EnhancedReActAgent _agent;
    private readonly MyClawConfiguration _config;
    private readonly MemoryStore _memoryStore;

    public MyClawAgent(
        MyClawConfiguration config,
        IModel model,
        MemoryStore memoryStore,
        SkillManager? skillManager = null)
    {
        _config = config;
        _memoryStore = memoryStore;

        var systemPrompt = BuildSystemPrompt();

        var builder = EnhancedReActAgent.Builder()
            .Name("MyClaw")
            .Model(model)
            .SysPrompt(systemPrompt)
            .MaxIterations(config.Agent.MaxToolIterations)
            .Verbose(config.Agent.Verbose);

        if (skillManager != null)
        {
            foreach (var skill in skillManager.LoadedSkills)
            {
                builder.AddTool(new SkillTool(skill));
            }
        }

        _agent = builder.Build();
    }

    public async Task<string> ChatAsync(string message, string sessionId = "default")
    {
        var msg = Msg.Builder()
            .Role("user")
            .TextContent(message)
            .AddMetadata("session_id", sessionId)
            .Build();

        var response = await _agent.Call(msg).FirstAsync();
        return response.GetTextContent() ?? "无响应";
    }

    private string BuildSystemPrompt()
    {
        var parts = new List<string>();

        var workspace = _config.Agent.Workspace;

        var agentsPath = Path.Combine(workspace, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            parts.Add(File.ReadAllText(agentsPath));
        }

        var soulPath = Path.Combine(workspace, "SOUL.md");
        if (File.Exists(soulPath))
        {
            parts.Add(File.ReadAllText(soulPath));
        }

        var heartbeatPath = Path.Combine(workspace, "HEARTBEAT.md");
        if (File.Exists(heartbeatPath))
        {
            parts.Add("## 心跳任务\n" + File.ReadAllText(heartbeatPath));
        }

        var memoryContext = _memoryStore.GetMemoryContext();
        if (!string.IsNullOrEmpty(memoryContext))
        {
            parts.Add("## 记忆上下文\n" + memoryContext);
        }

        parts.Add(@"
你是 MyClaw，一个个人 AI 助手。

你可以使用以下工具来完成任务：
- Skills: 各种专业领域的技能助手
- Calculator: 数学计算
- GetTime: 获取当前时间

请用中文或用户使用的语言回复。
");

        return string.Join("\n\n", parts);
    }
}
```

---

### 5. 可选：更新 ProviderConfig (`src/MyClaw.Core/Configuration/MyClawConfiguration.cs`)

确保配置类支持新字段：

```csharp
public class ProviderConfig
{
    public string? Type { get; set; }
    public string? Model { get; set; }
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
}

public class AgentConfig
{
    public string Workspace { get; set; } = "./workspace";
    public int MaxToolIterations { get; set; } = 10;
    public bool Verbose { get; set; } = false;
}
```

---

### 6. 配置示例 (config.example.json)

```json
{
  "provider": {
    "type": "deepseek",
    "model": "deepseek-chat",
    "apiKey": "${DEEPSEEK_API_KEY}"
  },
  "agent": {
    "workspace": "./workspace",
    "maxToolIterations": 10,
    "verbose": false
  }
}
```

---

## 📌 使用说明

**支持的模型类型**：

| 类型 | 默认模型 | 说明 |
|------|---------|------|
| `openai` | gpt-4o | OpenAI GPT 系列 |
| `azure` | gpt-4o | Azure OpenAI |
| `anthropic` | claude-sonnet-4-5-20250929 | Anthropic Claude |
| `deepseek` | deepseek-chat | DeepSeek |
| `gemini` | gemini-2.0-flash-exp | Google Gemini |
| `dashscope` | qwen-turbo | 阿里云通义千问 |
| `ollama` | llama3 | 本地 Ollama |

**CLI 使用示例**：
```bash
myclaw agent -m "你好"              # 单消息模式
myclaw agent                        # REPL 模式
myclaw agent --repl                 # 强制 REPL 模式
myclaw agent -m "你好" --model deepseek  # 指定模型
```