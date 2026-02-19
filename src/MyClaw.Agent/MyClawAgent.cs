using System.Reactive.Linq;
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using MyClaw.Core.Configuration;
using MyClaw.Memory;
using MyClaw.Skills;

namespace MyClaw.Agent;

/// <summary>
/// MyClaw Agent - 基于 AgentScope.NET 的个人 AI 助手
/// </summary>
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
            .MaxIterations(config.Agent.MaxToolIterations);

        // 添加 Skills 作为 Tools
        if (skillManager != null)
        {
            foreach (var skill in skillManager.LoadedSkills)
            {
                builder.AddTool(new SkillTool(skill));
            }
        }

        _agent = builder.Build();
    }

    /// <summary>
    /// 发送消息给 Agent 并获取响应
    /// </summary>
    public async Task<string> ChatAsync(string message, string sessionId = "default")
    {
        var msg = Msg.Builder()
            .Role("user")
            .TextContent(message)
            .AddMetadata("session_id", sessionId)
            .Build();

        var response = await _agent.Call(msg).FirstAsync();
        return response.GetTextContent() ?? "No response";
    }

    /// <summary>
    /// 构建系统提示词
    /// </summary>
    private string BuildSystemPrompt()
    {
        var parts = new List<string>();

        // AGENTS.md
        var agentsPath = Path.Combine(_config.Agent.Workspace, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            parts.Add(File.ReadAllText(agentsPath));
        }

        // SOUL.md
        var soulPath = Path.Combine(_config.Agent.Workspace, "SOUL.md");
        if (File.Exists(soulPath))
        {
            parts.Add(File.ReadAllText(soulPath));
        }

        // Memory 上下文
        var memoryContext = _memoryStore.GetMemoryContext();
        if (!string.IsNullOrEmpty(memoryContext))
        {
            parts.Add(memoryContext);
        }

        // 默认提示词
        parts.Add(@"
你是 MyClaw，一个个人 AI 助手。
You are MyClaw, a personal AI assistant.

你可以使用以下工具来辅助完成任务：
- Skills: 各种专业领域的技能助手

请用中文或用户使用的语言回复。
");

        return string.Join("\n\n", parts);
    }
}
