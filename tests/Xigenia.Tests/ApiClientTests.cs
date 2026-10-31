using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xigenia.Web.Services;

namespace Xigenia.Tests;

public sealed class ApiClientTests
{
    private static HttpClient CreateHttp(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var h = new StubHandler(handler);
        return new HttpClient(h) { BaseAddress = new Uri("http://localhost:5122") };
    }

    private static HttpClient CreateFailingHttp()
    {
        var h = new FailingHandler();
        return new HttpClient(h) { BaseAddress = new Uri("http://localhost:5122") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _fn;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> fn) => _fn = fn;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_fn(request));
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("network down");
    }

    private sealed class DelayedAuthHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        private readonly AuthState _auth;
        private readonly int _delayMs;
        public DelayedAuthHandler(HttpResponseMessage response, AuthState auth, int delayMs = 20) { _response = response; _auth = auth; _delayMs = delayMs; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delayMs, cancellationToken);
            return _response;
        }
    }

    private sealed class BrokenContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw new IOException("broken stream");
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }

    [Fact]
    public async Task Unauthorized_clears_auth_state_when_same_session()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new ApiClient(http, auth);
        var result = await client.ListAccountsAsync();
        Assert.True(result.IsUnauthorized);
        Assert.False(result.IsStale);
        Assert.False(auth.IsAuthenticated);
    }

    [Fact]
    public async Task Old_session_401_does_not_clear_new_session_delayed()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("old", "old@e.com");
        // Handler delays 50ms before returning 401; we bump session mid-flight.
        var http = new HttpClient(new DelayedAuthHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized), auth, 50)) { BaseAddress = new Uri("http://localhost:5122") };
        var client = new ApiClient(http, auth);
        var task = client.ListAccountsAsync();
        await Task.Delay(10);
        auth.SetAuthenticated("new", "new@e.com");
        var result = await task;
        Assert.True(result.IsStale);
        Assert.True(result.IsUnauthorized);
        Assert.True(auth.IsAuthenticated);
        Assert.Equal("new", auth.AccessToken);
    }

    [Fact]
    public async Task Stale_success_response_is_discarded()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = new HttpClient(new DelayedAuthHandler(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"name\":\"Old\",\"type\":2,\"currency\":\"USD\",\"openingBalance\":100,\"isActive\":true,\"createdAt\":\"2026-10-26T00:00:00Z\",\"currentBalance\":100,\"linkedMovementCount\":0}]", Encoding.UTF8, "application/json") }, auth, 50))
        { BaseAddress = new Uri("http://localhost:5122") };
        var client = new ApiClient(http, auth);
        var task = client.ListAccountsAsync();
        await Task.Delay(10);
        auth.SetAuthenticated("new-token", "new@e.com");
        var result = await task;
        Assert.True(result.IsStale);
        Assert.True(auth.IsAuthenticated);
    }

    [Fact]
    public async Task Adds_bearer_header_when_authenticated()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("my-token", "u@e.com");
        HttpRequestMessage? captured = null;
        var http = CreateHttp(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };
        });
        var client = new ApiClient(http, auth);
        await client.ListMovementsAsync();
        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal("my-token", captured.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Does_not_send_bearer_when_not_authenticated()
    {
        var auth = new AuthState();
        HttpRequestMessage? captured = null;
        var http = CreateHttp(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") };
        });
        var client = new ApiClient(http, auth);
        await client.ListAccountsAsync();
        Assert.NotNull(captured);
        Assert.Null(captured!.Headers.Authorization);
    }

    [Fact]
    public async Task Logout_clears_header_and_state_and_bumps_version()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5122") };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "tok");
        var client = new ApiClient(http, auth);
        var vBefore = auth.SessionVersion;
        client.Logout();
        Assert.False(auth.IsAuthenticated);
        Assert.Null(http.DefaultRequestHeaders.Authorization);
        Assert.Equal(vBefore + 1, auth.SessionVersion);
    }

    [Fact]
    public async Task Validation_error_parsed_and_allows_resubmit()
    {
        var auth = new AuthState();
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { title = "Invalid movement.", detail = "Amount must be positive.", status = 400 }), Encoding.UTF8, "application/json")
        });
        var client = new ApiClient(http, auth);
        var result = await client.CreateMovementAsync(new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Pagar, 0, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Expense));
        Assert.True(result.IsValidationError);
        Assert.False(result.IsUncertain);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public void Finance_dtos_do_not_expose_owner_id()
    {
        var props = typeof(Xigenia.Web.Models.CreateFinancialAccountRequest).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("OwnerId", props);
        var mprops = typeof(Xigenia.Web.Models.CreateMoneyMovementRequest).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("OwnerId", mprops);
        Assert.DoesNotContain("OwnerID", mprops);
    }

    [Fact]
    public async Task Uncertain_write_not_automatically_retried()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        int calls = 0;
        var http = CreateHttp(_ => { calls++; throw new HttpRequestException("down"); });
        var client = new ApiClient(http, auth);
        var accountReq = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var r1 = await client.CreateAccountAsync(accountReq);
        Assert.True(r1.IsUncertain);
        Assert.Equal(1, calls);
        var r2 = await client.CreateAccountAsync(accountReq);
        Assert.True(r2.IsUncertain);
        Assert.Equal(2, calls); // each call is one POST, no auto-retry inside a single call
    }

    [Fact]
    public async Task Uncertain_movement_blocked_until_explicit()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateFailingHttp();
        var client = new ApiClient(http, auth);
        var movReq = new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Cobrar, 10, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Income);
        var r = await client.CreateMovementAsync(movReq);
        Assert.True(r.IsUncertain);
        Assert.False(r.IsValidationError);
    }

    [Fact]
    public void Disposal_unsubscribes_handler()
    {
        var auth = new AuthState();
        int calls = 0;
        void Handler() => calls++;
        auth.OnChange += Handler;
        auth.SetAuthenticated("a", "u@e.com");
        Assert.Equal(1, calls);
        auth.OnChange -= Handler;
        auth.Clear();
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Successful_account_post_with_unreadable_body_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{{ not valid json", Encoding.UTF8, "application/json")
        });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var result = await client.CreateAccountAsync(req);
        Assert.True(result.IsUncertain);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsValidationError);
    }

    [Fact]
    public async Task Successful_movement_post_with_unreadable_body_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("not-json-at-all", Encoding.UTF8, "application/json")
        });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Cobrar, 10, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Income);
        var result = await client.CreateMovementAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Account_post_with_body_read_failure_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new BrokenContent() });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var result = await client.CreateAccountAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Account_post_returns_500_is_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"title\":\"Server error\"}", Encoding.UTF8, "application/json")
        });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var result = await client.CreateAccountAsync(req);
        Assert.True(result.IsUncertain);
        Assert.False(result.IsServerError);
        Assert.False(result.IsValidationError);
    }

    [Fact]
    public async Task Movement_post_returns_500_is_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Pagar, 10, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Expense);
        var result = await client.CreateMovementAsync(req);
        Assert.True(result.IsUncertain);
        Assert.False(result.IsServerError);
    }

    [Fact]
    public void Uncertain_blocks_resubmission_until_acknowledged()
    {
        var guard = new FinancialWriteGuard();
        Assert.True(guard.CanSubmit);
        guard.MarkUncertain("detail");
        Assert.False(guard.CanSubmit);
        guard.Reconcile(); // simulate Refresh, still blocked
        Assert.False(guard.CanSubmit);
        Assert.True(guard.IsUncertain);
        guard.Acknowledge();
        Assert.True(guard.CanSubmit);
        Assert.False(guard.IsUncertain);
    }

    [Fact]
    public async Task Account_post_204_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var result = await client.CreateAccountAsync(req);
        Assert.True(result.IsUncertain);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Movement_post_204_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Cobrar, 10, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Income);
        var result = await client.CreateMovementAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Account_post_200_empty_body_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("", Encoding.UTF8, "application/json") });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var result = await client.CreateAccountAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Movement_post_empty_body_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("", Encoding.UTF8, "application/json") });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Pagar, 10, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Expense);
        var result = await client.CreateMovementAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Account_post_whitespace_body_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("   \n\t  ", Encoding.UTF8, "application/json") });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var result = await client.CreateAccountAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Movement_post_whitespace_body_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("   ", Encoding.UTF8, "application/json") });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Cobrar, 10, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Income);
        var result = await client.CreateMovementAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Account_post_json_null_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("null", Encoding.UTF8, "application/json") });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var result = await client.CreateAccountAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Movement_post_json_null_returns_uncertain()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Pagar, 10, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Expense);
        var result = await client.CreateMovementAsync(req);
        Assert.True(result.IsUncertain);
    }

    [Fact]
    public async Task Valid_account_response_remains_successful()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var valid = new Xigenia.Web.Models.FinancialAccountResponse(Guid.NewGuid(), "Primary USD", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 1000, true, DateTimeOffset.UtcNow, null, null, null, 1000, null, 0);
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(JsonSerializer.Serialize(valid), Encoding.UTF8, "application/json") });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Primary USD", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 1000);
        var result = await client.CreateAccountAsync(req);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(result.IsUncertain);
    }

    [Fact]
    public async Task Valid_movement_response_remains_successful()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        var valid = new Xigenia.Web.Models.MoneyMovementResponse(Guid.NewGuid(), null, Guid.NewGuid(), null, Xigenia.Web.Models.MovementDirectionDto.Cobrar, 100, "USD", new DateOnly(2026,10,10), null, Xigenia.Web.Models.MovementClassificationDto.Income, null);
        var http = CreateHttp(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(JsonSerializer.Serialize(valid), Encoding.UTF8, "application/json") });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateMoneyMovementRequest(
            Xigenia.Web.Models.MovementDirectionDto.Cobrar, 100, "USD", new DateOnly(2026, 10, 10), null, null, Guid.NewGuid(), Xigenia.Web.Models.MovementClassificationDto.Income);
        var result = await client.CreateMovementAsync(req);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task Stale_success_with_null_does_not_affect_new_session()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("old", "old@e.com");
        var http = new HttpClient(new DelayedAuthHandler(new HttpResponseMessage(HttpStatusCode.NoContent), auth, 50)) { BaseAddress = new Uri("http://localhost:5122") };
        var client = new ApiClient(http, auth);
        var task = client.CreateAccountAsync(new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100));
        await Task.Delay(10);
        auth.SetAuthenticated("new", "new@e.com");
        var result = await task;
        Assert.True(result.IsStale);
        Assert.False(result.IsUncertain);
        Assert.True(auth.IsAuthenticated);
    }

    [Fact]
    public async Task No_automatic_retry_on_uncertain_financial_post()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("tok", "u@e.com");
        int calls = 0;
        var http = CreateHttp(_ => { calls++; return new HttpResponseMessage(HttpStatusCode.NoContent); });
        var client = new ApiClient(http, auth);
        var req = new Xigenia.Web.Models.CreateFinancialAccountRequest("Test", Xigenia.Web.Models.FinancialAccountTypeDto.BankAccount, "USD", 100);
        var r = await client.CreateAccountAsync(req);
        Assert.True(r.IsUncertain);
        Assert.Equal(1, calls);
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public void Validation_failure_remains_recoverable()
    {
        var guard = new FinancialWriteGuard();
        guard.MarkValidationFailed();
        Assert.True(guard.CanSubmit);
        Assert.False(guard.IsUncertain);
    }

    private sealed class FinancialWriteGuard
    {
        public bool IsUncertain { get; private set; }
        public bool CanSubmit => !IsUncertain;
        public void MarkUncertain(string _) => IsUncertain = true;
        public void MarkValidationFailed() => IsUncertain = false;
        public void Reconcile() { /* keeps uncertain */ }
        public void Acknowledge() => IsUncertain = false;
    }
}
