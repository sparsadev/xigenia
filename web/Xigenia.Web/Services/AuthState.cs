namespace Xigenia.Web.Services;

public sealed class AuthState
{
    private string? _accessToken;
    private string? _email;
    private int _sessionVersion;

    public string? AccessToken => _accessToken;
    public string? Email => _email;
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_accessToken);
    public int SessionVersion => _sessionVersion;

    public event Action? OnChange;

    public void SetAuthenticated(string accessToken, string email)
    {
        _accessToken = accessToken;
        _email = email;
        _sessionVersion++;
        Notify();
    }

    public void Clear()
    {
        var wasAuthenticated = IsAuthenticated;
        _accessToken = null;
        _email = null;
        _sessionVersion++;
        // Always notify so views clear immediately; version bump isolates stale responses.
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}
