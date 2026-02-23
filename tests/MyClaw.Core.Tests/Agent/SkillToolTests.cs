using MyClaw.Agent;
using MyClaw.Skills;
using AgentScope.Core.Tool;

namespace MyClaw.Core.Tests.Agent;

public class SkillToolTests
{
    [Fact]
    public void Constructor_WithSkill_ShouldSetNameAndDescription()
    {
        var skill = new Skill
        {
            Name = "writer",
            Description = "Writing assistant skill",
            Content = "You are a writer assistant."
        };

        var tool = new SkillTool(skill);

        Assert.Equal("writer", tool.Name);
        Assert.Equal("Writing assistant skill", tool.Description);
    }

    [Fact]
    public void GetSchema_ShouldReturnCorrectSchema()
    {
        var skill = new Skill
        {
            Name = "calculator",
            Description = "Math calculation skill"
        };

        var tool = new SkillTool(skill);
        var schema = tool.GetSchema();

        Assert.NotNull(schema);
        Assert.Equal("object", schema["type"]);
        Assert.NotNull(schema["properties"]);
        Assert.NotNull(schema["required"]);
        
        var properties = (Dictionary<string, object>)schema["properties"];
        Assert.Contains("intent", properties);
        Assert.Contains("query", properties);
        
        var required = schema["required"];
        Assert.NotNull(required);
    }

    [Fact]
    public async Task ExecuteAsync_WithIntentAndQuery_ShouldReturnFormattedResult()
    {
        var skill = new Skill
        {
            Name = "web-search",
            Description = "Web search skill",
            Content = "You can search the web for information."
        };

        var tool = new SkillTool(skill);
        var parameters = new Dictionary<string, object>
        {
            ["intent"] = "search",
            ["query"] = "What is AgentScope?"
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        var content = result.Result.ToString();
        Assert.Contains("Skill: web-search", content);
        Assert.Contains("Intent: search", content);
        Assert.Contains("Query: What is AgentScope?", content);
        Assert.Contains("You can search the web for information.", content);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullIntent_ShouldHandleGracefully()
    {
        var skill = new Skill
        {
            Name = "writer",
            Description = "Writing skill"
        };

        var tool = new SkillTool(skill);
        var parameters = new Dictionary<string, object>
        {
            ["query"] = "Write a story"
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.Contains("Intent: ", result.Result.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullQuery_ShouldHandleGracefully()
    {
        var skill = new Skill
        {
            Name = "calculator",
            Description = "Calculation skill"
        };

        var tool = new SkillTool(skill);
        var parameters = new Dictionary<string, object>
        {
            ["intent"] = "calculate"
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.Contains("Query: ", result.Result.ToString());
    }
}
