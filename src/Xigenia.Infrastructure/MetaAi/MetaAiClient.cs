using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Xigenia.Infrastructure.MetaAi;

public sealed class MetaAiClient : IMetaAiClient
{
    private readonly HttpClient _http;
    private readonly MetaAiOptions _options;
    private readonly ILogger<MetaAiClient> _logger;

    public MetaAiClient(HttpClient http, IOptions<MetaAiOptions> options, ILogger<MetaAiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MetaAiResult> CreateResponseAsync(string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MetaAiException("Meta AI provider is not configured.", 503);
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input must be provided.", nameof(input));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // OpenAI-compatible Chat Completions: deterministic financial context as bounded messages
        var systemMessage = "You are Xigenia, a helpful personal finance assistant. Explain only from verified records. Do not invent numbers. Keep explanation concise, grounded, and separate facts from suggestions. Label AI output as not authoritative.";
        var payload = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = input }
            },
            max_tokens = _options.MaxOutputTokens,
            stream = false
        };

        request.Content = JsonContent.Create(payload, options: null);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout
            _logger.LogWarning(ex, "Meta AI request timed out after {Timeout}s", _options.TimeoutSeconds);
            throw new MetaAiException("Meta AI request timed out.", 504, ex);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Meta AI transport failure");
            throw new MetaAiException("Meta AI transport failure.", null, ex);
        }

        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Meta AI provider returned {StatusCode}", (int)response.StatusCode);
            // Do not expose provider body/headers
            throw new MetaAiException($"Meta AI provider error ({(int)response.StatusCode}).", (int)response.StatusCode);
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawBody);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Meta AI returned malformed JSON");
            throw new MetaAiException("Meta AI returned malformed response.", null, ex);
        }

        using (doc)
        {
            var root = doc.RootElement;

            // Reject provider error indicators even when HTTP 200 and choices present
            if (root.TryGetProperty("error", out var errorProp) && errorProp.ValueKind != JsonValueKind.Null && errorProp.ValueKind != JsonValueKind.Undefined)
            {
                _logger.LogWarning("Meta AI returned error response");
                throw new MetaAiException("Meta AI returned error response.", null);
            }

            // OpenAI-compatible Chat Completions: choices[].message.content
            if (!root.TryGetProperty("choices", out var choicesProp) || choicesProp.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Meta AI response missing choices");
                throw new MetaAiException("Meta AI returned malformed response (missing choices).", null);
            }
            if (choicesProp.GetArrayLength() == 0)
            {
                _logger.LogWarning("Meta AI response choices empty");
                throw new MetaAiException("Meta AI response has no choices.", null);
            }

            var firstChoice = choicesProp[0];
            if (!firstChoice.TryGetProperty("message", out var messageProp) || messageProp.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning("Meta AI response choice missing message");
                throw new MetaAiException("Meta AI response missing message.", null);
            }
            if (!messageProp.TryGetProperty("content", out var contentProp) || contentProp.ValueKind != JsonValueKind.String)
            {
                _logger.LogWarning("Meta AI response message missing content");
                throw new MetaAiException("Meta AI response missing content.", null);
            }
            var text = contentProp.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Meta AI response contained no content");
                throw new MetaAiException("Meta AI response contained no output.", null);
            }

            // Handle truncated/incomplete: finish_reason must be "stop" for completed
            string? finishReason = null;
            if (firstChoice.TryGetProperty("finish_reason", out var frProp) && frProp.ValueKind == JsonValueKind.String)
            {
                finishReason = frProp.GetString();
            }
            else if (firstChoice.TryGetProperty("finish_reason", out var fr2) && fr2.ValueKind == JsonValueKind.Null)
            {
                finishReason = null;
            }

            if (string.IsNullOrWhiteSpace(finishReason))
            {
                _logger.LogWarning("Meta AI response missing finish_reason");
                throw new MetaAiException("Meta AI response missing finish_reason.", null);
            }
            if (!string.Equals(finishReason, "stop", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Meta AI returned non-stop finish_reason {FinishReason}", finishReason);
                return new MetaAiResult(string.Empty, ExtractModel(root), TryGetId(root), true, finishReason);
            }

            var model = ExtractModel(root);
            var id = TryGetId(root);
            return new MetaAiResult(text, model, id, false, finishReason);
        }
    }

    private string ExtractModel(JsonElement root)
    {
        if (root.TryGetProperty("model", out var m) && m.ValueKind == JsonValueKind.String)
        {
            return m.GetString() ?? _options.Model;
        }
        return _options.Model;
    }

    private static string? TryGetId(JsonElement root)
    {
        if (root.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
        {
            return id.GetString();
        }
        return null;
    }

    private static string ExtractOutputText(JsonElement root)
    {
        var builder = new System.Text.StringBuilder();

        // Primary: output[] -> message -> content[] -> output_text
        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in output.EnumerateArray())
            {
                // Some implementations use "output" items directly containing content
                if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    foreach (var block in content.EnumerateArray())
                    {
                        if (block.TryGetProperty("type", out var type) && type.GetString() == "output_text"
                            && block.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                        {
                            builder.Append(textProp.GetString());
                        }
                        else if (block.TryGetProperty("text", out var t2) && t2.ValueKind == JsonValueKind.String
                                 && block.TryGetProperty("type", out var ttype) && ttype.GetString() == "text")
                        {
                            builder.Append(t2.GetString());
                        }
                    }
                }
                // Fallback: item itself is output_text
                if (item.TryGetProperty("type", out var itemType) && itemType.GetString() == "output_text"
                    && item.TryGetProperty("text", out var itemText) && itemText.ValueKind == JsonValueKind.String)
                {
                    builder.Append(itemText.GetString());
                }
            }
        }

        if (builder.Length > 0)
        {
            return builder.ToString();
        }

        // Fallback: top-level output_text or text
        if (root.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
        {
            return ot.GetString() ?? string.Empty;
        }
        if (root.TryGetProperty("text", out var txt) && txt.ValueKind == JsonValueKind.String)
        {
            return txt.GetString() ?? string.Empty;
        }

        // Last resort: first string value in output
        return string.Empty;
    }
}
