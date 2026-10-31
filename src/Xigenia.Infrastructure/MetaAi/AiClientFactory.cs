using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Xigenia.Infrastructure.MetaAi;

/// <summary>Explicit provider factory. Selects exactly one configured provider; never falls back automatically.</summary>
public interface IAiClientFactory
{
    /// <summary>Active provider name: "Meta" or "Baseten".</summary>
    string Provider { get; }

    /// <summary>Normalized options bag for the active provider only.</summary>
    MetaAiOptions ActiveOptions { get; }

    IMetaAiClient CreateClient();
}

public sealed class AiClientFactory : IAiClientFactory
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AiProviderOptions _provider;
    private readonly MetaStandardOptions _meta;
    private readonly MetaAiOptions _baseten;
    private readonly ILogger<MetaAiClient> _logger;

    public const string MetaHttpClientName = "MetaAiStandard";
    public const string BasetenHttpClientName = "Baseten";

    public AiClientFactory(
        IHttpClientFactory httpFactory,
        IOptions<AiProviderOptions> provider,
        IOptions<MetaStandardOptions> meta,
        IOptions<MetaAiOptions> baseten,
        ILogger<MetaAiClient> logger)
    {
        _httpFactory = httpFactory;
        _provider = provider.Value;
        _meta = meta.Value;
        _baseten = baseten.Value;
        _logger = logger;
    }

    public string Provider => _provider.IsBaseten ? AiProviderOptions.Baseten : AiProviderOptions.Meta;

    public MetaAiOptions ActiveOptions
    {
        get
        {
            if (_provider.IsBaseten)
            {
                return _baseten;
            }

            return new MetaAiOptions
            {
                BaseUrl = _meta.BaseUrl,
                Model = _meta.Model,
                ApiKey = _meta.ApiKey,
                TimeoutSeconds = _meta.TimeoutSeconds,
                MaxOutputTokens = _meta.MaxOutputTokens,
                ReasoningEffort = _meta.ReasoningEffort
            };
        }
    }

    public IMetaAiClient CreateClient()
    {
        var active = ActiveOptions;
        var name = _provider.IsBaseten ? BasetenHttpClientName : MetaHttpClientName;
        var http = _httpFactory.CreateClient(name);

        var raw = string.IsNullOrWhiteSpace(active.BaseUrl) ? BaseUrlDefault() : active.BaseUrl.Trim();
        if (!raw.EndsWith('/'))
        {
            raw += "/";
        }
        http.BaseAddress = new Uri(raw, UriKind.Absolute);
        http.Timeout = TimeSpan.FromSeconds(active.TimeoutSeconds > 0 ? active.TimeoutSeconds : 30);

        return new MetaAiClient(http, Options.Create(active), _logger);
    }

    private string BaseUrlDefault() => _provider.IsBaseten
        ? "https://inference.baseten.co/v1/"
        : "https://api.meta.ai/v1/";
}
