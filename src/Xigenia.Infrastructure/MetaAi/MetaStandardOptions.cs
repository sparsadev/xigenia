namespace Xigenia.Infrastructure.MetaAi;

/// <summary>Server-side options for the official Meta Model API Standard provider.</summary>
public sealed class MetaStandardOptions
{
    public const string SectionName = "MetaAi";

    /// <summary>Base URL for Meta Model API, e.g. https://api.meta.ai/v1/</summary>
    public string BaseUrl { get; set; } = "https://api.meta.ai/v1/";

    /// <summary>Model identifier, e.g. muse-spark-1.3</summary>
    public string Model { get; set; } = "muse-spark-1.3";

    /// <summary>Server-side Bearer key. Must be provided via User Secrets or environment variable, never in repo.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Bounded timeout in seconds for provider call.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Bounded max output tokens for non-streaming response.</summary>
    public int MaxOutputTokens { get; set; } = 10000;

    /// <summary>Reasoning effort for Meta Standard Chat Completions, e.g. "low".</summary>
    public string ReasoningEffort { get; set; } = "low";
}
