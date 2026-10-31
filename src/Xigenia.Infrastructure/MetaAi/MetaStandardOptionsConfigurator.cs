using Microsoft.Extensions.Configuration;

namespace Xigenia.Infrastructure.MetaAi;

public static class MetaStandardOptionsConfigurator
{
    public const string StandardModel = "muse-spark-1.3";

    public static void Configure(MetaStandardOptions options, IConfiguration configuration)
    {
        // Bind only MetaAi section - no fallback to Baseten:ApiKey (credential isolation)
        configuration.GetSection(MetaStandardOptions.SectionName).Bind(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            options.BaseUrl = "https://api.meta.ai/v1/";
        }

        // Enforce Standard-only routing: muse-spark-1.3 is the sole supported Meta model.
        // Known obsolete identifiers normalize to Standard; anything else is rejected here,
        // before any provider HTTP request can be built.
        if (string.IsNullOrWhiteSpace(options.Model))
        {
            options.Model = StandardModel;
        }
        else
        {
            options.Model = options.Model.Trim();
        }

        if (options.Model == StandardModel)
        {
            // Valid Standard configuration; nothing further to do.
        }
        else if (options.Model == "muse-spark-1.3-contributor" ||
                 options.Model == "meta-models/muse-spark-open" ||
                 options.Model == "meta-models/muse-spark-open-1026")
        {
            options.Model = StandardModel;
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported Meta model '{options.Model}'. The only supported Meta model is '{StandardModel}'.");
        }

        if (options.TimeoutSeconds <= 0) options.TimeoutSeconds = 30;
        if (options.MaxOutputTokens <= 0) options.MaxOutputTokens = 10000;
        if (string.IsNullOrWhiteSpace(options.ReasoningEffort)) options.ReasoningEffort = "low";
    }
}
