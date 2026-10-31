using Microsoft.Extensions.Configuration;

namespace Xigenia.Infrastructure.MetaAi;

public static class MetaAiOptionsConfigurator
{
    public static void Configure(MetaAiOptions options, IConfiguration configuration)
    {
        // Bind only Baseten section - no fallback to MetaAi:ApiKey (credential isolation)
        configuration.GetSection("Baseten").Bind(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            options.BaseUrl = "https://inference.baseten.co/v1/";
        }

        // Migrate legacy Meta default URL if explicitly configured via Baseten (e.g., old value copied)
        if (options.BaseUrl.Trim().Equals("https://api.meta.ai/v1", StringComparison.OrdinalIgnoreCase) ||
            options.BaseUrl.Trim().Equals("https://api.meta.ai/v1/", StringComparison.OrdinalIgnoreCase))
        {
            options.BaseUrl = "https://inference.baseten.co/v1/";
        }

        if (string.IsNullOrWhiteSpace(options.Model) || options.Model == "muse-spark-1.3")
        {
            options.Model = "meta-models/muse-spark-open";
        }

        if (options.TimeoutSeconds <= 0) options.TimeoutSeconds = 30;
        if (options.MaxOutputTokens <= 0) options.MaxOutputTokens = 512;
    }
}
