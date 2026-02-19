using MyClaw.Core.Configuration;
using System.Text.Json;

namespace MyClaw.Core.Tests.Configuration;

public class ConfigurationLoaderTests : IDisposable
{
    private readonly string _tempConfigDir;
    private readonly string _tempConfigPath;
    private readonly string? _originalOpenAIKey;
    private readonly string? _originalDeepSeekKey;
    private readonly string? _originalAnthropicKey;

    public ConfigurationLoaderTests()
    {
        _tempConfigDir = Path.Combine(Path.GetTempPath(), $"myclaw_test_config_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempConfigDir);
        _tempConfigPath = Path.Combine(_tempConfigDir, "config.json");
        
        // 保存并清除环境变量
        _originalOpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _originalDeepSeekKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        _originalAnthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", null);
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
    }

    public void Dispose()
    {
        // 恢复环境变量
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", _originalOpenAIKey);
        Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", _originalDeepSeekKey);
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", _originalAnthropicKey);
        
        if (Directory.Exists(_tempConfigDir))
            Directory.Delete(_tempConfigDir, true);
    }

    [Fact]
    public void Load_Defaults_ShouldCreateDefaultConfig()
    {
        var config = ConfigurationLoader.Load(_tempConfigPath);

        Assert.NotNull(config);
        Assert.NotEmpty(config.Provider.Type);
        Assert.NotEmpty(config.Agent.Workspace);
    }

    [Fact]
    public void Load_ExistingConfig_ShouldLoadExisting()
    {
        // 先创建配置文件
        var existingConfig = new MyClawConfiguration
        {
            Provider = new ProviderConfig { Type = "custom", ApiKey = "test-key", BaseUrl = "https://test.com" },
            Agent = new AgentConfig { Model = "custom-model" }
        };
        
        File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(existingConfig));

        var loaded = ConfigurationLoader.Load(_tempConfigPath);

        Assert.Equal("custom", loaded.Provider.Type);
        Assert.Equal("test-key", loaded.Provider.ApiKey);
        Assert.Equal("custom-model", loaded.Agent.Model);
    }

    [Fact]
    public void Save_Config_ShouldPersistToFile()
    {
        var config = new MyClawConfiguration
        {
            Provider = new ProviderConfig { Type = "test", ApiKey = "key123" }
        };

        ConfigurationLoader.Save(config, _tempConfigPath);

        Assert.True(File.Exists(_tempConfigPath));
        var content = File.ReadAllText(_tempConfigPath);
        Assert.Contains("test", content);
        Assert.Contains("key123", content);
    }

    [Fact]
    public void DetectEnvironmentVariables_OpenAI_ShouldDetectOpenAI()
    {
        try
        {
            // 设置环境变量
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-openai-key");
            Environment.SetEnvironmentVariable("OPENAI_BASE_URL", "https://custom.openai.com");

            var config = ConfigurationLoader.Load(_tempConfigPath);

            Assert.Equal("openai", config.Provider.Type);
            Assert.Equal("test-openai-key", config.Provider.ApiKey);
            Assert.Equal("https://custom.openai.com", config.Provider.BaseUrl);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("OPENAI_BASE_URL", null);
        }
    }

    [Fact]
    public void DetectEnvironmentVariables_DeepSeek_ShouldDetectDeepSeek()
    {
        try
        {
            // 设置环境变量
            Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", "test-deepseek-key");

            var config = ConfigurationLoader.Load(_tempConfigPath);

            Assert.Equal("deepseek", config.Provider.Type);
            Assert.Equal("test-deepseek-key", config.Provider.ApiKey);
            Assert.Equal("https://api.deepseek.com/v1", config.Provider.BaseUrl);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", null);
        }
    }

    [Fact]
    public void Load_Save_RoundTrip_ShouldPreserveData()
    {
        var original = new MyClawConfiguration
        {
            Provider = new ProviderConfig { Type = "test-type", ApiKey = "secret", BaseUrl = "http://test" },
            Agent = new AgentConfig { Model = "gpt-test", MaxTokens = 2000, Temperature = 0.5f },
            Tools = new ToolsConfig { BraveApiKey = "brave-key", ExecTimeout = 60 }
        };

        ConfigurationLoader.Save(original, _tempConfigPath);
        var loaded = ConfigurationLoader.Load(_tempConfigPath);

        Assert.Equal(original.Provider.Type, loaded.Provider.Type);
        Assert.Equal(original.Provider.ApiKey, loaded.Provider.ApiKey);
        Assert.Equal(original.Agent.Model, loaded.Agent.Model);
        Assert.Equal(original.Agent.MaxTokens, loaded.Agent.MaxTokens);
        Assert.Equal(original.Tools.BraveApiKey, loaded.Tools.BraveApiKey);
    }
}
