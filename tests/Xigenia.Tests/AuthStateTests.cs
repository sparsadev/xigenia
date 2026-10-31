using Xigenia.Web.Services;

namespace Xigenia.Tests;

public sealed class AuthStateTests
{
    [Fact]
    public void SetAuthenticated_makes_authenticated_and_bumps_version()
    {
        var state = new AuthState();
        Assert.False(state.IsAuthenticated);
        var v0 = state.SessionVersion;
        state.SetAuthenticated("access", "user@example.com");
        Assert.True(state.IsAuthenticated);
        Assert.Equal("access", state.AccessToken);
        Assert.Equal("user@example.com", state.Email);
        Assert.Equal(v0 + 1, state.SessionVersion);
    }

    [Fact]
    public void Clear_removes_token_and_requires_sign_in_and_bumps_version()
    {
        var state = new AuthState();
        state.SetAuthenticated("tok", "a@b.com");
        var v1 = state.SessionVersion;
        var changed = false;
        state.OnChange += () => changed = true;
        state.Clear();
        Assert.False(state.IsAuthenticated);
        Assert.Null(state.AccessToken);
        Assert.Null(state.Email);
        Assert.Equal(v1 + 1, state.SessionVersion);
        Assert.True(changed);
    }

    [Fact]
    public void Bearer_is_memory_only_not_persisted()
    {
        var state = new AuthState();
        state.SetAuthenticated("tok123", "x@y.com");
        var fresh = new AuthState();
        Assert.False(fresh.IsAuthenticated);
        Assert.Null(fresh.AccessToken);
    }

    [Fact]
    public void Does_not_retain_refresh_token()
    {
        var props = typeof(AuthState).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("RefreshToken", props);
        var fields = typeof(AuthState).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Select(f => f.Name).ToList();
        Assert.DoesNotContain("_refreshToken", fields);
    }

    [Fact]
    public void Version_increments_on_each_auth_change()
    {
        var state = new AuthState();
        var v0 = state.SessionVersion;
        state.SetAuthenticated("a", "u@e.com");
        state.Clear();
        state.SetAuthenticated("b", "v@e.com");
        Assert.Equal(v0 + 3, state.SessionVersion);
    }
}
