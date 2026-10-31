using System.Net;
using System.Text;
using Xigenia.Web.Models;
using Xigenia.Web.Services;

namespace Xigenia.Tests;

public sealed class AiExplanationTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest;
        public string? CapturedBody;
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _fn;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> fn) => _fn = fn;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            CapturedBody = request.Content?.ReadAsStringAsync().Result;
            return Task.FromResult(_fn(request));
        }
    }

    private static (ApiClient Client, AuthState Auth, StubHandler Handler) Create(
        Func<HttpRequestMessage, HttpResponseMessage> fn, string token = "tok")
    {
        var auth = new AuthState();
        auth.SetAuthenticated(token, "user@example.com");
        var handler = new StubHandler(fn);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5122") };
        return (new ApiClient(http, auth), auth, handler);
    }

    private static HttpResponseMessage OkExplanation(string explanation, string disclaimer = "Not authoritative.") =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"explanation\":\"" + explanation +
                "\",\"model\":\"muse-spark-1.3\",\"provider\":\"meta\"," +
                "\"contextTimestamp\":\"2026-10-26T00:00:00Z\",\"generatedAt\":\"2026-10-26T00:01:00Z\"," +
                "\"isAuthoritativeFinancialCalculation\":false,\"disclaimer\":\"" + disclaimer + "\"}",
                Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task Sends_authenticated_post_with_question_and_maps_response()
    {
        var (client, _, handler) = Create(_ => OkExplanation("Spending is steady.", "Verify with your records."));
        var result = await client.RequestFinancialExplanationAsync(new FinancialExplanationRequest("Where did my money go?"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal(HttpMethod.Post, handler.CapturedRequest!.Method);
        Assert.EndsWith("/api/ai/financial-explanation", handler.CapturedRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.CapturedRequest.Headers.Authorization!.Scheme);
        Assert.Equal("tok", handler.CapturedRequest.Headers.Authorization.Parameter);
        Assert.Contains("Where did my money go?", handler.CapturedBody);
        Assert.Equal("Spending is steady.", result.Data!.Explanation);
        Assert.Equal("muse-spark-1.3", result.Data.Model);
        Assert.Equal("meta", result.Data.Provider);
        Assert.False(result.Data.IsAuthoritativeFinancialCalculation);
        Assert.Equal("Verify with your records.", result.Data.Disclaimer);
    }

    [Fact]
    public async Task No_provider_credentials_enter_client_output()
    {
        var (client, _, handler) = Create(_ => OkExplanation("ok"));
        var result = await client.RequestFinancialExplanationAsync(new FinancialExplanationRequest("Summarize please"));

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("ApiKey", handler.CapturedBody ?? "");
        Assert.DoesNotContain("tok", result.Data!.Explanation);
        Assert.DoesNotContain("tok", result.Data.Disclaimer);
        Assert.Equal("tok", handler.CapturedRequest!.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task Unauthorized_clears_auth_state()
    {
        var (client, auth, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var result = await client.RequestFinancialExplanationAsync(new FinancialExplanationRequest("Hi there?"));

        Assert.True(result.IsUnauthorized);
        Assert.False(auth.IsAuthenticated);
    }

    [Fact]
    public async Task Validation_error_for_invalid_question()
    {
        var (client, _, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                "{\"title\":\"Invalid question.\",\"detail\":\"Question is required.\",\"status\":400}",
                Encoding.UTF8, "application/json")
        });
        var result = await client.RequestFinancialExplanationAsync(new FinancialExplanationRequest("  "));

        Assert.True(result.IsValidationError);
        Assert.Contains("Question is required.", result.Error!.Detail ?? "");
    }

    [Fact]
    public async Task Server_error_maps_to_server_failed()
    {
        var (client, _, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("{\"title\":\"Provider error.\"}", Encoding.UTF8, "application/json")
        });
        var result = await client.RequestFinancialExplanationAsync(new FinancialExplanationRequest("Explain please"));

        Assert.True(result.IsServerError);
        Assert.False(result.IsUncertain);
    }

    [Fact]
    public async Task Timeout_maps_to_uncertain_retryable()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "user@example.com");
        var http = new HttpClient(new CancellingHandler()) { BaseAddress = new Uri("http://localhost:5122") };
        var client = new ApiClient(http, auth);

        var result = await client.RequestFinancialExplanationAsync(new FinancialExplanationRequest("Explain please"));

        Assert.True(result.IsUncertain);
        Assert.Contains("safe to try again", result.Error!.Detail ?? "");
        Assert.True(auth.IsAuthenticated);
    }

    [Fact]
    public async Task Empty_explanation_is_success_with_empty_data_for_ui_empty_state()
    {
        var (client, _, _) = Create(_ => OkExplanation(""));
        var result = await client.RequestFinancialExplanationAsync(new FinancialExplanationRequest("Explain please"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(string.IsNullOrWhiteSpace(result.Data!.Explanation));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.Disclaimer));
    }

    private sealed class CancellingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new TaskCanceledException("timed out");
    }
}
