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
