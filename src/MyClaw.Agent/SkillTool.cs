using AgentScope.Core.Tool;
using MyClaw.Skills;

namespace MyClaw.Agent;

/// <summary>
/// 将 Skill 转换为 AgentScope 的 ITool
/// </summary>
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
            ["properties"] = new Dictionary<string, object>(),
            ["required"] = new List<string>()
        };
    }

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        // Skill 作为系统提示词返回
        var prompt = _skill.GetSystemPrompt();
        return Task.FromResult(ToolResult.Ok(prompt));
    }
}
