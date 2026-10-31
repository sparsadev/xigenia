using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xigenia.Infrastructure.MetaAi;

namespace Xigenia.Tests;

public sealed class MetaStandardTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest;
        public string? CapturedBody;
        public int CallCount;
        private readonly HttpResponseMessage _response;
        public StubHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            CapturedRequest = request;
            CapturedBody = request.Content?.ReadAsStringAsync().Result;
            return Task.FromResult(_response);
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private static IConfiguration Config(params (string Key, string? Value)[] pairs)
    {
        var dict = pairs.ToDictionary(p => p.Key, p => p.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static HttpResponseMessage OkChat(string model, string? content, string finish = "stop")
    {
        var contentJson = content is null ? "null" : "\"" + content + "\"";
        var json = "{\"id\":\"chatcmpl-1\",\"model\":\"" + model
            + "\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":"
            + contentJson + "},\"finish_reason\":\"" + finish + "\"}]}";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public void Meta_standard_is_default_provider_with_standard_model()
    {
        var config = Config(("MetaAi:ApiKey", "meta-placeholder-1"));
        var provider = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(provider, config);
        var meta = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(meta, config);

        Assert.Equal("Meta", provider.Provider);
        Assert.False(provider.IsBaseten);
        Assert.Equal("muse-spark-1.3", meta.Model);
        Assert.Equal("https://api.meta.ai/v1/", meta.BaseUrl);
        Assert.Equal("meta-placeholder-1", meta.ApiKey);
        Assert.Equal(10000, meta.MaxOutputTokens);
        Assert.Equal("low", meta.ReasoningEffort);
        Assert.Equal(30, meta.TimeoutSeconds);
    }

    [Fact]
    public async Task Meta_request_uses_standard_endpoint_and_model()
    {
        var config = Config(
            ("Ai:Provider", "Meta"),
            ("MetaAi:ApiKey", "meta-key-abc"),
            ("Baseten:ApiKey", "baseten-key-should-not-be-used"));
        var providerOpts = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(providerOpts, config);
        var metaOpts = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(metaOpts, config);
        var basetenOpts = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(basetenOpts, config);

        var handler = new StubHandler(OkChat("muse-spark-1.3", "ok"));
        var factory = new AiClientFactory(
            new StubHttpClientFactory(handler),
            Options.Create(providerOpts),
            Options.Create(metaOpts),
            Options.Create(basetenOpts),
            NullLogger<MetaAiClient>.Instance);

        Assert.Equal("Meta", factory.Provider);
        var active = factory.ActiveOptions;
        Assert.Equal("meta-key-abc", active.ApiKey);
        Assert.Equal("muse-spark-1.3", active.Model);

        var client = factory.CreateClient();
        var result = await client.CreateResponseAsync("Explain my spending", CancellationToken.None);

        Assert.Equal("ok", result.Text);
        Assert.False(result.IsIncomplete);
        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal(HttpMethod.Post, handler.CapturedRequest!.Method);
        Assert.EndsWith("chat/completions", handler.CapturedRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.CapturedRequest.Headers.Authorization!.Scheme);
        Assert.Equal("meta-key-abc", handler.CapturedRequest.Headers.Authorization.Parameter);
        Assert.Contains("\"model\":\"muse-spark-1.3\"", handler.CapturedBody);
        Assert.Contains("\"role\":\"system\"", handler.CapturedBody);
        Assert.Contains("\"role\":\"user\"", handler.CapturedBody);
        Assert.Contains("\"stream\":false", handler.CapturedBody);
        Assert.Contains("\"max_tokens\":10000", handler.CapturedBody);
        Assert.Contains("\"reasoning_effort\":\"low\"", handler.CapturedBody);
        Assert.DoesNotContain("baseten-key-should-not-be-used", handler.CapturedBody);
        Assert.DoesNotContain("\"store\"", handler.CapturedBody);
    }

    [Fact]
    public void Contributor_model_is_normalized_to_standard()
    {
        var config = Config(
            ("MetaAi:ApiKey", "meta-key-1"),
            ("MetaAi:Model", "muse-spark-1.3-contributor"));
        var meta = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(meta, config);
        Assert.Equal("muse-spark-1.3", meta.Model);
        Assert.Equal("meta-key-1", meta.ApiKey);
    }

    [Fact]
    public async Task Explicit_standard_model_is_preserved_and_sent()
    {
        var config = Config(
            ("Ai:Provider", "Meta"),
            ("MetaAi:ApiKey", "meta-key-1"),
            ("MetaAi:Model", "muse-spark-1.3"));
        var providerOpts = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(providerOpts, config);
        var metaOpts = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(metaOpts, config);
        Assert.Equal("muse-spark-1.3", metaOpts.Model);

        var basetenOpts = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(basetenOpts, config);
        var handler = new StubHandler(OkChat("muse-spark-1.3", "ok"));
        var factory = new AiClientFactory(
            new StubHttpClientFactory(handler),
            Options.Create(providerOpts),
            Options.Create(metaOpts),
            Options.Create(basetenOpts),
            NullLogger<MetaAiClient>.Instance);
        var client = factory.CreateClient();
        await client.CreateResponseAsync("q", CancellationToken.None);
        Assert.Contains("\"model\":\"muse-spark-1.3\"", handler.CapturedBody);
    }

    [Fact]
    public void Legacy_1_2_contributor_is_rejected()
    {
        var config = Config(
            ("MetaAi:ApiKey", "meta-key-1"),
            ("MetaAi:Model", "muse-spark-1.2-contributor"));
        var meta = new MetaStandardOptions();
        var ex = Assert.Throws<InvalidOperationException>(() => MetaStandardOptionsConfigurator.Configure(meta, config));
        Assert.Contains("muse-spark-1.3", ex.Message);
    }

    [Fact]
    public void Arbitrary_unsupported_meta_model_is_rejected()
    {
        var config = Config(
            ("MetaAi:ApiKey", "meta-key-1"),
            ("MetaAi:Model", "custom-model-xyz"));
        var meta = new MetaStandardOptions();
        Assert.Throws<InvalidOperationException>(() => MetaStandardOptionsConfigurator.Configure(meta, config));
    }

    [Fact]
    public void Invalid_meta_model_fails_before_provider_http_request()
    {
        var config = Config(
            ("Ai:Provider", "Meta"),
            ("MetaAi:ApiKey", "meta-key-1"),
            ("MetaAi:Model", "muse-spark-1.2-contributor"),
            ("Baseten:ApiKey", "baseten-key-1"));
        var providerOpts = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(providerOpts, config);
        var basetenOpts = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(basetenOpts, config);

        var handler = new StubHandler(OkChat("muse-spark-1.3", "ok"));

        // Production wiring order: Meta options configure before any factory/client exists.
        var metaOpts = new MetaStandardOptions();
        Assert.Throws<InvalidOperationException>(() => MetaStandardOptionsConfigurator.Configure(metaOpts, config));

        // No client was constructed, so no provider HTTP request could occur.
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public void Unknown_provider_is_rejected()
    {
        var config = Config(("Ai:Provider", "Bogus"));
        var provider = new AiProviderOptions();
        Assert.Throws<InvalidOperationException>(() => AiProviderOptionsConfigurator.Configure(provider, config));
    }

    [Fact]
    public void Baseten_key_never_configures_meta()
    {
        var config = Config(("Baseten:ApiKey", "baseten-only-key"));
        var meta = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(meta, config);
        Assert.True(string.IsNullOrWhiteSpace(meta.ApiKey));
    }

    [Fact]
    public void Meta_key_never_configures_baseten()
    {
        var config = Config(("MetaAi:ApiKey", "meta-only-key"));
        var baseten = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(baseten, config);
        Assert.True(string.IsNullOrWhiteSpace(baseten.ApiKey));
    }

    [Fact]
    public async Task Missing_selected_meta_key_prevents_call_even_when_baseten_present()
    {
        var config = Config(
            ("Ai:Provider", "Meta"),
            ("Baseten:ApiKey", "baseten-present-key"));
        var providerOpts = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(providerOpts, config);
        var metaOpts = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(metaOpts, config);
        var basetenOpts = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(basetenOpts, config);

        var handler = new StubHandler(OkChat("muse-spark-1.3", "ok"));
        var factory = new AiClientFactory(
            new StubHttpClientFactory(handler),
            Options.Create(providerOpts),
            Options.Create(metaOpts),
            Options.Create(basetenOpts),
            NullLogger<MetaAiClient>.Instance);

        Assert.True(string.IsNullOrWhiteSpace(factory.ActiveOptions.ApiKey));
        var client = factory.CreateClient();
        var ex = await Assert.ThrowsAsync<MetaAiException>(() => client.CreateResponseAsync("hi", CancellationToken.None));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Length_finish_with_null_content_is_incomplete_not_malformed()
    {
        var opts = Options.Create(new MetaAiOptions
        {
            ApiKey = "meta-key-1",
            BaseUrl = "https://api.meta.ai/v1/",
            Model = "muse-spark-1.3"
        });
        var handler = new StubHandler(OkChat("muse-spark-1.3", null, "length"));
        var client = new MetaAiClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.meta.ai/v1/") },
            opts, NullLogger<MetaAiClient>.Instance);

        var result = await client.CreateResponseAsync("question", CancellationToken.None);
        Assert.True(result.IsIncomplete);
        Assert.Equal("length", result.Status);
    }

    [Fact]
    public async Task Explicit_baseten_selection_retains_contract()
    {
        var config = Config(
            ("Ai:Provider", "Baseten"),
            ("Baseten:ApiKey", "baseten-key-1"),
            ("MetaAi:ApiKey", "meta-key-should-not-be-used"));
        var providerOpts = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(providerOpts, config);
        var metaOpts = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(metaOpts, config);
        var basetenOpts = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(basetenOpts, config);

        var handler = new StubHandler(OkChat("meta-models/muse-spark-open-1026", "ok"));
        var factory = new AiClientFactory(
            new StubHttpClientFactory(handler),
            Options.Create(providerOpts),
            Options.Create(metaOpts),
            Options.Create(basetenOpts),
            NullLogger<MetaAiClient>.Instance);

        Assert.Equal("Baseten", factory.Provider);
        Assert.Equal("meta-models/muse-spark-open-1026", factory.ActiveOptions.Model);
        Assert.Equal("baseten-key-1", factory.ActiveOptions.ApiKey);

        var client = factory.CreateClient();
        var result = await client.CreateResponseAsync("q", CancellationToken.None);
        Assert.Equal("ok", result.Text);
        Assert.Equal("Bearer", handler.CapturedRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("baseten-key-1", handler.CapturedRequest.Headers.Authorization.Parameter);
        Assert.Contains("meta-models/muse-spark-open-1026", handler.CapturedBody);
        Assert.DoesNotContain("meta-key-should-not-be-used", handler.CapturedBody ?? "");
        // Baseten contract unchanged: 512-token limit, no reasoning_effort field.
        Assert.Contains("\"max_tokens\":512", handler.CapturedBody);
        Assert.DoesNotContain("reasoning_effort", handler.CapturedBody ?? "");
    }

    [Fact]
    public async Task Baseten_request_is_unchanged_by_meta_token_configuration()
    {
        var config = Config(
            ("Ai:Provider", "Baseten"),
            ("Baseten:ApiKey", "baseten-key-1"),
            ("MetaAi:ApiKey", "meta-key-1"),
            ("MetaAi:MaxOutputTokens", "10000"),
            ("MetaAi:ReasoningEffort", "low"));
        var providerOpts = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(providerOpts, config);
        var metaOpts = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(metaOpts, config);
        var basetenOpts = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(basetenOpts, config);

        var handler = new StubHandler(OkChat("meta-models/muse-spark-open-1026", "ok"));
        var factory = new AiClientFactory(
            new StubHttpClientFactory(handler),
            Options.Create(providerOpts),
            Options.Create(metaOpts),
            Options.Create(basetenOpts),
            NullLogger<MetaAiClient>.Instance);

        var client = factory.CreateClient();
        await client.CreateResponseAsync("q", CancellationToken.None);
        Assert.Contains("\"max_tokens\":512", handler.CapturedBody);
        Assert.DoesNotContain("reasoning_effort", handler.CapturedBody ?? "");
        Assert.DoesNotContain("10000", handler.CapturedBody ?? "");
    }

    [Fact]
    public async Task No_automatic_fallback_single_provider_call()
    {
        var config = Config(
            ("Ai:Provider", "Meta"),
            ("MetaAi:ApiKey", "meta-key-1"),
            ("Baseten:ApiKey", "baseten-key-1"));
        var providerOpts = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(providerOpts, config);
        var metaOpts = new MetaStandardOptions();
        MetaStandardOptionsConfigurator.Configure(metaOpts, config);
        var basetenOpts = new MetaAiOptions();
        MetaAiOptionsConfigurator.Configure(basetenOpts, config);

        var handler = new StubHandler(OkChat("muse-spark-1.3", "ok"));
        var factory = new AiClientFactory(
            new StubHttpClientFactory(handler),
            Options.Create(providerOpts),
            Options.Create(metaOpts),
            Options.Create(basetenOpts),
            NullLogger<MetaAiClient>.Instance);

        var client = factory.CreateClient();
        await client.CreateResponseAsync("q", CancellationToken.None);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("meta-key-1", handler.CapturedRequest!.Headers.Authorization!.Parameter);
    }

    [Fact]
    public void Provider_selection_is_case_insensitive_and_explicit()
    {
        var lower = Config(("Ai:Provider", "baseten"));
        var p1 = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(p1, lower);
        Assert.Equal("Baseten", p1.Provider);

        var upper = Config(("Ai:Provider", "META"));
        var p2 = new AiProviderOptions();
        AiProviderOptionsConfigurator.Configure(p2, upper);
        Assert.Equal("Meta", p2.Provider);
    }
}
