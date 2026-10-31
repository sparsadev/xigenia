using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xigenia.Application.Finance;
using Xigenia.Domain;
using Xigenia.Infrastructure.MetaAi;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Xigenia.Tests;

public sealed class BasetenCredentialIsolationTests
{
    private static IOptions<MetaAiOptions> BuildOptions(IConfiguration config)
    {
        // Exercise actual production registration logic, not a copy
        var opts = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(opts, config);
        return Options.Create(opts);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest;
        public string? CapturedBody;
        private readonly HttpResponseMessage _response;
        public StubHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CapturedRequest = request;
            CapturedBody = request.Content?.ReadAsStringAsync().Result;
            return Task.FromResult(_response);
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages = new();
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }

    [Fact]
    public void Legacy_meta_key_does_not_configure_baseten()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MetaAi:ApiKey"] = "legacy-placeholder-abc123",
                ["Baseten:ApiKey"] = ""
            })
            .Build();

        var opts = BuildOptions(config).Value;

        Assert.True(string.IsNullOrWhiteSpace(opts.ApiKey), "Baseten ApiKey should remain unconfigured when only legacy Meta key is present");
        Assert.NotEqual("legacy-placeholder-abc123", opts.ApiKey);
    }

    [Fact]
    public void Legacy_meta_key_present_baseten_absent_remains_unconfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MetaAi:ApiKey"] = "legacy-placeholder-xyz789",
                // Baseten:ApiKey missing
            })
            .Build();

        var opts = BuildOptions(config).Value;
        Assert.True(string.IsNullOrWhiteSpace(opts.ApiKey));
    }

    [Fact]
    public void Valid_baseten_key_is_used()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Baseten:ApiKey"] = "baseten-valid-placeholder-456",
                ["MetaAi:ApiKey"] = "legacy-placeholder-123"
            })
            .Build();

        var opts = BuildOptions(config).Value;
        Assert.Equal("baseten-valid-placeholder-456", opts.ApiKey);
        Assert.NotEqual("legacy-placeholder-123", opts.ApiKey);
    }

    [Fact]
    public async Task Missing_baseten_key_prevents_provider_call_and_returns_503_sanitized()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Baseten:ApiKey"] = "",
                ["MetaAi:ApiKey"] = "legacy-placeholder-should-not-be-used"
            })
            .Build();

        var opts = BuildOptions(config);
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"x\",\"model\":\"meta-models/muse-spark-open-1026\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"hi\"},\"finish_reason\":\"stop\"}]}", Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://inference.baseten.co/v1/") };
        var logger = new CapturingLogger<MetaAiClient>();
        var client = new MetaAiClient(http, opts, logger);

        var ex = await Assert.ThrowsAsync<MetaAiException>(() => client.CreateResponseAsync("test input", CancellationToken.None));
        Assert.Equal(503, ex.StatusCode);
        Assert.Null(handler.CapturedRequest); // no HTTP call made when unconfigured
        Assert.DoesNotContain("legacy-placeholder-should-not-be-used", string.Join(" ", logger.Messages));
        Assert.DoesNotContain("legacy-placeholder-should-not-be-used", ex.Message);
    }

    [Fact]
    public async Task Valid_baseten_key_is_sent_as_bearer_and_not_legacy()
    {
        var opts = Options.Create(new MetaAiOptions
        {
            ApiKey = "baseten-valid-placeholder-789",
            BaseUrl = "https://inference.baseten.co/v1/",
            Model = "meta-models/muse-spark-open-1026"
        });
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"chatcmpl-1\",\"model\":\"meta-models/muse-spark-open-1026\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"ok\"},\"finish_reason\":\"stop\"}]}", Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://inference.baseten.co/v1/") };
        var logger = new CapturingLogger<MetaAiClient>();
        var client = new MetaAiClient(http, opts, logger);

        var result = await client.CreateResponseAsync("hello", CancellationToken.None);

        Assert.Equal("ok", result.Text);
        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal("Bearer", handler.CapturedRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("baseten-valid-placeholder-789", handler.CapturedRequest.Headers.Authorization.Parameter);
        Assert.DoesNotContain("legacy-placeholder", handler.CapturedBody ?? "");
        // Logs must not contain ApiKey
        Assert.DoesNotContain("baseten-valid-placeholder-789", string.Join(" ", logger.Messages));
    }

    [Fact]
    public async Task No_credential_in_client_error_or_logs()
    {
        var opts = Options.Create(new MetaAiOptions
        {
            ApiKey = "baseten-secret-placeholder-999",
            BaseUrl = "https://inference.baseten.co/v1/",
            Model = "meta-models/muse-spark-open-1026"
        });
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"error\":\"internal\"}", Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://inference.baseten.co/v1/") };
        var logger = new CapturingLogger<MetaAiClient>();
        var client = new MetaAiClient(http, opts, logger);

        var ex = await Assert.ThrowsAsync<MetaAiException>(() => client.CreateResponseAsync("input", CancellationToken.None));
        Assert.DoesNotContain("baseten-secret-placeholder-999", ex.Message);
        Assert.DoesNotContain("baseten-secret-placeholder-999", string.Join(" ", logger.Messages));
        // Also ensure Authorization header not logged via ToString
        Assert.DoesNotContain("baseten-secret-placeholder-999", handler.CapturedBody ?? "");
    }

    [Fact]
    public void Baseten_url_and_model_defaults_preserved()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Baseten:ApiKey"] = "baseten-valid-placeholder-111"
            })
            .Build();
        var opts = BuildOptions(config).Value;
        Assert.Equal("https://inference.baseten.co/v1/", opts.BaseUrl);
        Assert.Equal("meta-models/muse-spark-open-1026", opts.Model);
    }

    [Fact]
    public void Legacy_baseurl_does_not_override_baseten()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Baseten:ApiKey"] = "baseten-key",
                ["Baseten:BaseUrl"] = "https://api.meta.ai/v1",
                ["MetaAi:BaseUrl"] = "https://api.meta.ai/v1"
            })
            .Build();
        var opts = BuildOptions(config).Value;
        Assert.Equal("https://inference.baseten.co/v1/", opts.BaseUrl); // migrated
    }
}
