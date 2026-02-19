using MyClaw.Core.Configuration;
using MyClaw.Core.Entities;
using MyClaw.Memory;
using MyClaw.Core.Evolution;
using System.Text.Json;

namespace MyClaw.Integration.Tests.EndToEnd;

public class WorkflowTests : IDisposable
{
    private readonly string _tempBaseDir;
    private readonly string _configPath;
    private readonly string _workspaceDir;
    private readonly string? _originalOpenAIKey;
    private readonly string? _originalDeepSeekKey;

    public WorkflowTests()
    {
        _tempBaseDir = Path.Combine(Path.GetTempPath(), $"myclaw_e2e_{Guid.NewGuid()}");
        _configPath = Path.Combine(_tempBaseDir, "config.json");
        _workspaceDir = Path.Combine(_tempBaseDir, "workspace");
        Directory.CreateDirectory(_tempBaseDir);
        Directory.CreateDirectory(_workspaceDir);
        
        // 保存原始环境变量
        _originalOpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _originalDeepSeekKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        
        // 清除环境变量以避免干扰测试
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", null);
    }

    public void Dispose()
    {
        // 恢复原始环境变量
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", _originalOpenAIKey);
        Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", _originalDeepSeekKey);
        
        if (Directory.Exists(_tempBaseDir))
            Directory.Delete(_tempBaseDir, true);
    }

    [Fact]
    public void FullConfigurationFlow_ShouldInitializeAllComponents()
    {
        // 1. 创建配置
        var config = new MyClawConfiguration
        {
            Provider = new ProviderConfig { Type = "openai", ApiKey = "test-key" },
            Agent = new AgentConfig { Model = "gpt-4", Workspace = _workspaceDir }
        };
        
        // 2. 保存并加载配置
        ConfigurationLoader.Save(config, _configPath);
        var loaded = ConfigurationLoader.Load(_configPath);

        Assert.NotNull(loaded);
        Assert.Equal("openai", loaded.Provider.Type);
        Assert.Equal("test-key", loaded.Provider.ApiKey);
    }

    [Fact]
    public async Task MemoryEntityInteractionFlow_ShouldWorkTogether()
    {
        // 1. 创建存储
        var memoryStore = new MemoryStore(_workspaceDir);
        var entityStore = new EntityStore(_workspaceDir);

        // 2. 写入记忆
        memoryStore.WriteLongTerm("讨论使用 React 开发项目");
        memoryStore.AppendToday("Today's conversation about React");

        // 3. 添加实体
        var entity = await entityStore.AddAsync(new Entity
        {
            Name = "React",
            Type = EntityType.Tool,
            Attributes = new Dictionary<string, string> { ["category"] = "frontend" }
        });
        Assert.NotNull(entity);
        Assert.Equal("React", entity.Name);
        Assert.Equal(EntityType.Tool, entity.Type);

        // 4. 验证记忆
        var longTerm = memoryStore.ReadLongTerm();
        Assert.Contains("React", longTerm);

        // 5. 获取相关实体
        var relevantEntities = await entityStore.SurfaceRelevantAsync("使用 React 开发项目");
        Assert.Single(relevantEntities);
        Assert.Equal("React", relevantEntities[0].Name);
    }

    [Fact]
    public void SignalDetectionFlow_ShouldDetectAndRespond()
    {
        // 模拟用户输入包含信号
        var detector = new SignalDetector();
        
        var userInputs = new[]
        {
            "我喜欢使用 dark theme",  // UserPreference
            "别那么严肃",              // PersonalityCorrection
            "项目用的是 Python"        // EnvironmentConfig
        };

        var allSignals = new List<DetectedSignal>();
        foreach (var input in userInputs)
        {
            var signals = detector.DetectSignals(input);
            allSignals.AddRange(signals);
        }

        Assert.Contains(allSignals, s => s.SignalType == EvolutionSignal.UserPreference);
        Assert.Contains(allSignals, s => s.SignalType == EvolutionSignal.PersonalityCorrection);
        Assert.Contains(allSignals, s => s.SignalType == EvolutionSignal.EnvironmentConfig);
    }

    [Fact]
    public void SignalDetector_ShouldReturnCorrectTargetFiles()
    {
        var detector = new SignalDetector();
        
        var preferenceSignals = detector.DetectSignals("我喜欢 dark theme");
        Assert.All(preferenceSignals, s => Assert.Equal("USER.md", s.TargetFile));

        var personalitySignals = detector.DetectSignals("别那么严肃");
        Assert.All(personalitySignals, s => Assert.Equal("SOUL.md", s.TargetFile));

        var configSignals = detector.DetectSignals("项目用的是 Python");
        Assert.All(configSignals, s => Assert.Equal("TOOLS.md", s.TargetFile));
    }

    [Fact]
    public void SignalDetector_ShouldSuggestCorrectTools()
    {
        var detector = new SignalDetector();
        
        var signals = detector.DetectSignals("我喜欢使用 dark theme");
        Assert.All(signals, s => Assert.Equal("miniclaw_update", s.SuggestedTool));
    }

    [Fact]
    public async Task EntityStore_ShouldTrackMentionCounts()
    {
        var entityStore = new EntityStore(_workspaceDir);

        // 添加实体
        var entity = await entityStore.AddAsync(new Entity { Name = "TypeScript", Type = EntityType.Tool });
        Assert.Equal(1, entity.MentionCount);

        // 再次添加（模拟再次提及）
        var updated = await entityStore.AddAsync(new Entity { Name = "TypeScript", Type = EntityType.Tool });
        Assert.Equal(2, updated.MentionCount);

        // 查询验证
        var queried = await entityStore.QueryAsync("TypeScript");
        Assert.NotNull(queried);
        Assert.Equal(2, queried.MentionCount);
    }

    [Fact]
    public async Task EntityStore_ShouldManageRelations()
    {
        var entityStore = new EntityStore(_workspaceDir);

        // 添加实体
        await entityStore.AddAsync(new Entity 
        { 
            Name = "React", 
            Type = EntityType.Tool,
            Relations = new List<string> { "used-by:MyProject" }
        });

        // 添加关系
        await entityStore.LinkAsync("React", "requires:TypeScript");

        // 验证
        var entity = await entityStore.QueryAsync("React");
        Assert.NotNull(entity);
        Assert.Contains("used-by:MyProject", entity.Relations);
        Assert.Contains("requires:TypeScript", entity.Relations);
    }

    [Fact]
    public void MemoryStore_ShouldGetRecentMemories()
    {
        var memoryStore = new MemoryStore(_workspaceDir);

        // 创建长期记忆
        memoryStore.WriteLongTerm("Important long-term knowledge");
        
        // 创建日记条目
        memoryStore.AppendToday("Today's activities");

        // 获取上下文
        var context = memoryStore.GetMemoryContext();
        
        Assert.Contains("长期记忆", context);
        Assert.Contains("Important long-term knowledge", context);
        Assert.Contains("最近日志", context);
        Assert.Contains("Today's activities", context);
    }

    [Fact]
    public void Configuration_Defaults_ShouldBeValid()
    {
        var config = MyClawConfiguration.Default();

        Assert.NotNull(config.Provider);
        Assert.NotNull(config.Agent);
        Assert.NotNull(config.Channels);
        Assert.NotNull(config.Tools);
        Assert.NotNull(config.Skills);
        Assert.NotNull(config.Gateway);

        Assert.False(string.IsNullOrEmpty(config.Agent.Workspace));
        Assert.True(config.Agent.MaxTokens > 0);
        Assert.True(config.Agent.Temperature >= 0);
    }
}
