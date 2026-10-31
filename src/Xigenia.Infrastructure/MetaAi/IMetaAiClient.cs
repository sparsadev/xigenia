namespace Xigenia.Infrastructure.MetaAi;

public interface IMetaAiClient
{
    /// <summary>
    /// Creates a non-streaming financial explanation via Meta Responses API.
    /// Input must be a bounded prompt containing only deterministic financial facts.
    /// </summary>
    Task<MetaAiResult> CreateResponseAsync(string input, CancellationToken cancellationToken);
}

public sealed record MetaAiResult(
    string Text,
    string Model,
    string? ResponseId,
    bool IsIncomplete,
    string? Status);

public sealed class MetaAiException : Exception
{
    public int? StatusCode { get; }

    public MetaAiException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}
