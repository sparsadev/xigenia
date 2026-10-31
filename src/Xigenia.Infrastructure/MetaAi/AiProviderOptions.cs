using Microsoft.Extensions.Configuration;

namespace Xigenia.Infrastructure.MetaAi;

/// <summary>Explicit, configuration-driven AI provider selection. No automatic fallback.</summary>
public sealed class AiProviderOptions
{
    public const string SectionName = "Ai";
    public const string Meta = "Meta";
    public const string Baseten = "Baseten";

    /// <summary>Active provider: "Meta" (default) or "Baseten".</summary>
    public string Provider { get; set; } = Meta;

    public bool IsBaseten => string.Equals(Provider, Baseten, StringComparison.OrdinalIgnoreCase);
}

public static class AiProviderOptionsConfigurator
{
    public static void Configure(AiProviderOptions options, IConfiguration configuration)
    {
        configuration.GetSection(AiProviderOptions.SectionName).Bind(options);

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            options.Provider = AiProviderOptions.Meta;
            return;
        }

        options.Provider = options.Provider.Trim();

        if (string.Equals(options.Provider, AiProviderOptions.Baseten, StringComparison.OrdinalIgnoreCase))
        {
            options.Provider = AiProviderOptions.Baseten;
        }
        else if (string.Equals(options.Provider, AiProviderOptions.Meta, StringComparison.OrdinalIgnoreCase))
        {
            options.Provider = AiProviderOptions.Meta;
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported AI provider '{options.Provider}'. Supported values: 'Meta', 'Baseten'.");
        }
    }
}
