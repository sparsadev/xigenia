namespace Xigenia.Infrastructure.MetaAi;

public sealed class MetaAiOptions
{
    public const string SectionName = "Baseten";

    /// <summary>Base URL for Baseten inference, e.g. https://inference.baseten.co/v1/</summary>
    public string BaseUrl { get; set; } = "https://inference.baseten.co/v1/";

    /// <summary>Model identifier, e.g. meta-models/muse-spark-open-1026</summary>
    public string Model { get; set; } = "meta-models/muse-spark-open-1026";

    /// <summary>Server-side bearer API key. Must be provided via User Secrets or environment variable, never in repo.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Bounded timeout in seconds for provider call.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Bounded max output tokens for non-streaming response.</summary>
    public int MaxOutputTokens { get; set; } = 512;
}
