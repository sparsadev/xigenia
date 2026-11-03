namespace Xigenia.Web.Services;

public sealed class ProfileMutationState
{
    private readonly AuthState _auth;

    public event Action? OnChange;

    public ProfileMutationState(AuthState auth)
    {
        _auth = auth;
        _auth.OnChange += HandleAuthChange;
    }

    public void NotifyChanged() => OnChange?.Invoke();

    private void HandleAuthChange()
    {
        if (_ownerVersion != _auth.SessionVersion)
        {
            ClearForNewSession();
        }
    }

    private int? _ownerVersion;
    public int ProfileRevision { get; private set; }

    public void IncrementProfileRevision()
    {
        ProfileRevision++;
        NotifyChanged();
    }

    // Password change. No credential values are stored here; only status.
    public bool ChangeBusy { get; set; }
    public int? ChangeBusyVersion { get; set; }
    public bool ChangeUncertain { get; set; }
    public string? ChangeUncertainDetail { get; set; }
    public string? ChangeSuccess { get; set; }
    public string? ChangeError { get; set; }

    // Email-change request. Only the requested address is retained for pending display.
    public bool EmailBusy { get; set; }
    public int? EmailBusyVersion { get; set; }
    public bool EmailUncertain { get; set; }
    public string? EmailUncertainDetail { get; set; }
    public string? EmailSuccess { get; set; }
    public string? EmailError { get; set; }
    public string? RequestedEmail { get; set; }

    public void EnsureOwner()
    {
        if (_ownerVersion != _auth.SessionVersion)
        {
            ClearForNewSession();
            _ownerVersion = _auth.SessionVersion;
        }
    }

    private void ClearForNewSession()
    {
        ChangeBusy = false;
        ChangeBusyVersion = null;
        ChangeUncertain = false;
        ChangeUncertainDetail = null;
        ChangeSuccess = null;
        ChangeError = null;
        EmailBusy = false;
        EmailBusyVersion = null;
        EmailUncertain = false;
        EmailUncertainDetail = null;
        EmailSuccess = null;
        EmailError = null;
        RequestedEmail = null;
        _ownerVersion = _auth.SessionVersion;
        NotifyChanged();
    }

    public void AcknowledgeChangeUncertainty()
    {
        ChangeUncertain = false;
        ChangeUncertainDetail = null;
    }

    public void AcknowledgeEmailUncertainty()
    {
        EmailUncertain = false;
        EmailUncertainDetail = null;
    }
}
