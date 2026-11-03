using System.Net;
using System.Text;
using System.Text.Json;
using Xigenia.Web.Models;
using Xigenia.Web.Pages;
using Xigenia.Web.Services;

namespace Xigenia.Tests;

public sealed class ShowcaseR6ProfileTests
{
    private static readonly JsonSerializerOptions Camel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void ManageInfoPayloads_UseBackendContractNames()
    {
        var change = JsonSerializer.Serialize(new ChangePasswordRequest("old-pw", "new-pw-1"), Camel);
        Assert.Contains("\"newPassword\"", change);
        Assert.Contains("\"oldPassword\"", change);
        Assert.DoesNotContain("NewPassword", change.Replace("\"newPassword\"", ""));

        var email = JsonSerializer.Serialize(new RequestEmailChangeRequest("b@synthetic.invalid"), Camel);
        Assert.Contains("\"newEmail\"", email);
    }

    [Fact]
    public void ProfileMutationState_ClearsSessionOwnedStatusOnNewSession()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("token-a", "a@synthetic.invalid");
        var state = new ProfileMutationState(auth);
        state.EnsureOwner();
        state.ChangeUncertain = true;
        state.ChangeUncertainDetail = "detail-a";
        state.EmailUncertain = true;
        state.RequestedEmail = "b@synthetic.invalid";

        auth.SetAuthenticated("token-b", "b@synthetic.invalid");

        Assert.False(state.ChangeUncertain);
        Assert.Null(state.ChangeUncertainDetail);
        Assert.False(state.EmailUncertain);
        Assert.Null(state.RequestedEmail);
    }

    [Fact]
    public void ProfileMutationState_AcknowledgeClearsWithoutPosting()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("token-a", "a@synthetic.invalid");
        var state = new ProfileMutationState(auth);
        state.EnsureOwner();
        state.ChangeUncertain = true;
        state.ChangeUncertainDetail = "detail";
        state.EmailUncertain = true;
        state.EmailUncertainDetail = "detail";

        state.AcknowledgeChangeUncertainty();
        state.AcknowledgeEmailUncertainty();

        Assert.False(state.ChangeUncertain);
        Assert.False(state.EmailUncertain);
    }

    [Fact]
    public void ProfileMutationState_StoresNoCredentialStrings()
    {
        var members = typeof(ProfileMutationState).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name)
            .Concat(typeof(ProfileMutationState).GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(string))
                .Select(f => f.Name));
        Assert.DoesNotContain(members, n => n.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ChangePasswordAsync_PostsManageInfoAndAcceptsMatchingEmail()
    {
        var handler = new FakeProfileHandler(_ => Json(new { email = "a@synthetic.invalid", isEmailConfirmed = false }));
        var (api, _) = CreateClient(handler);
        var result = await api.ChangePasswordAsync(new ChangePasswordRequest("old-pw", "new-pw-1"));

        Assert.True(result.IsSuccess);
        Assert.Single(handler.Posts);
        Assert.Equal("/api/auth/manage/info", handler.Posts[0].Path);
        Assert.Contains("\"newPassword\"", handler.Posts[0].Body);
        Assert.Contains("\"oldPassword\"", handler.Posts[0].Body);
    }

    [Fact]
    public async Task ChangePasswordAsync_Maps400ToValidationNotUncertain()
    {
        var handler = new FakeProfileHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                "{\"title\":\"Invalid password.\",\"detail\":\"Password mismatch.\",\"status\":400,\"errors\":{\"PasswordMismatch\":[\"Incorrect password.\"]}}",
                Encoding.UTF8, "application/json"),
        });
        var (api, _) = CreateClient(handler);
        var result = await api.ChangePasswordAsync(new ChangePasswordRequest("wrong-pw", "new-pw-1"));

        Assert.True(result.IsValidationError);
        Assert.False(result.IsUncertain);
    }

    [Fact]
    public async Task ChangePasswordAsync_Maps500ToUncertain()
    {
        var handler = new FakeProfileHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("down"),
        });
        var (api, _) = CreateClient(handler);
        var result = await api.ChangePasswordAsync(new ChangePasswordRequest("old-pw", "new-pw-1"));

        Assert.True(result.IsUncertain);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RequestEmailChangeAsync_PostsNewEmailOnly()
    {
        var handler = new FakeProfileHandler(_ => Json(new { email = "a@synthetic.invalid", isEmailConfirmed = false }));
        var (api, _) = CreateClient(handler);
        var result = await api.RequestEmailChangeAsync(new RequestEmailChangeRequest("b@synthetic.invalid"));

        Assert.True(result.IsSuccess);
        Assert.Single(handler.Posts);
        Assert.Contains("\"newEmail\"", handler.Posts[0].Body);
        Assert.DoesNotContain("newPassword", handler.Posts[0].Body);
    }

    [Fact]
    public void HomeDispose_UnsubscribesBothAuthAndTransferState()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("token-a", "a@synthetic.invalid");
        var tx = new AccountTransferMutationState(auth);
        tx.EnsureOwner();
        var home = new Home();
        Set(home, "Auth", auth);
        Set(home, "AcctTxState", tx);

        var onAuth = (Action)Delegate.CreateDelegate(typeof(Action), home,
            typeof(Home).GetMethod("HandleAuthChange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);
        var onTx = (Action)Delegate.CreateDelegate(typeof(Action), home,
            typeof(Home).GetMethod("HandleAccountTransferStateChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);
        auth.OnChange += onAuth;
        tx.OnChange += onTx;

        home.Dispose();
        home.Dispose();

        Assert.DoesNotContain(onAuth, InvocationList(auth, "OnChange"));
        Assert.DoesNotContain(onTx, InvocationList(tx, "OnChange"));
    }

    private static Delegate[] InvocationList(object target, string eventName)
    {
        var field = target.GetType().GetField(eventName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return ((MulticastDelegate?)field?.GetValue(target))?.GetInvocationList() ?? Array.Empty<Delegate>();
    }

    private static (ApiClient Api, AuthState Auth) CreateClient(FakeProfileHandler handler)
    {
        var auth = new AuthState();
        auth.SetAuthenticated("token-a", "a@synthetic.invalid");
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return (new ApiClient(http, auth), auth);
    }

    private static void Set(object target, string name, object? value)
    {
        var prop = target.GetType().GetProperty(name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop is not null && prop.CanWrite)
        {
            prop.SetValue(target, value);
            return;
        }
        throw new Exception($"member {name} missing");
    }

    private static HttpResponseMessage Json(object payload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload, Camel), Encoding.UTF8, "application/json"),
    };

    private sealed class FakeProfileHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public List<(string Path, string Body)> Posts { get; } = new();

        public FakeProfileHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            if (request.Method == HttpMethod.Post) Posts.Add((path, body));
            return _respond(request);
        }
    }
}
