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
