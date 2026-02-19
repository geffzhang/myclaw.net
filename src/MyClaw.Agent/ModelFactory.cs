using AgentScope.Core.Model;
using AgentScope.Core.Model.Anthropic;
using AgentScope.Core.Model.OpenAI;
using MyClaw.Core.Configuration;

namespace MyClaw.Agent;

/// <summary>
/// 模型工厂 - 根据配置创建 LLM 模型
/// </summary>
public static class ModelFactory
{
    /// <summary>
    /// 根据配置创建模型
    /// </summary>
    public static IModel Create(ProviderConfig config)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            throw new InvalidOperationException("API key is required. Set MYCLAW_API_KEY or configure in config.json");
        }

        var providerType = config.Type?.ToLowerInvariant() ?? "anthropic";

        return providerType switch
        {
            "openai" => CreateOpenAIModel(config),
            "deepseek" => CreateDeepSeekModel(config),
            "anthropic" => CreateAnthropicModel(config),
            _ => throw new NotSupportedException($"Provider type not supported: {config.Type}")
        };
    }

    private static IModel CreateAnthropicModel(ProviderConfig config)
    {
        return new AnthropicModel(
            modelName: GetModelName(config, "claude-sonnet-4-5-20250929"),
            apiKey: config.ApiKey,
            baseUrl: config.BaseUrl
        );
    }

    private static IModel CreateOpenAIModel(ProviderConfig config)
    {
        return new OpenAIModel(
            modelName: GetModelName(config, "gpt-4o"),
            apiKey: config.ApiKey,
            baseUrl: !string.IsNullOrEmpty(config.BaseUrl) ? config.BaseUrl : "https://api.openai.com/v1"
        );
    }

    private static IModel CreateDeepSeekModel(ProviderConfig config)
    {
        // DeepSeek 使用 OpenAI 兼容的 API
        return new OpenAIModel(
            modelName: GetModelName(config, "deepseek-chat"),
            apiKey: config.ApiKey,
            baseUrl: !string.IsNullOrEmpty(config.BaseUrl) ? config.BaseUrl : "https://api.deepseek.com/v1"
        );
    }

    private static string GetModelName(ProviderConfig config, string defaultModel)
    {
        // 这里可以从 AgentConfig 获取模型名称
        return defaultModel;
    }
}
