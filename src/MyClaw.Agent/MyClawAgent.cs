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
