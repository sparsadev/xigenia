using Xigenia.Web.Models;

namespace Xigenia.Web.Services;

public sealed class AccountTransferMutationState
{
    private readonly AuthState _auth;
    public event Action? OnChange;
    private int? _ownerVersion;

    public AccountTransferMutationState(AuthState auth)
    {
        _auth = auth;
        _auth.OnChange += HandleAuthChange;
    }

    private void HandleAuthChange()
    {
        if (_ownerVersion != _auth.SessionVersion)
        {
            ClearForNewSession();
        }
    }

    public void EnsureOwner()
    {
        if (_ownerVersion != _auth.SessionVersion)
        {
            ClearForNewSession();
            _ownerVersion = _auth.SessionVersion;
        }
    }

    // Account
    public bool AccountBusy { get; set; }
    public int? AccountBusyVersion { get; set; }
    public bool AccountUncertain { get; set; }
    public string? AccountUncertainDetail { get; set; }
    public string? AccountSuccess { get; set; }
    public string? AccountError { get; set; }

    // Transfer
    public bool TransferBusy { get; set; }
    public int? TransferBusyVersion { get; set; }
    public bool TransferUncertain { get; set; }
    public string? TransferUncertainDetail { get; set; }
    public string? TransferSuccess { get; set; }
    public string? TransferError { get; set; }
    public TransferResponse? LastTransferReceipt { get; set; }
    public Guid? LastSourceId { get; set; }
    public Guid? LastDestId { get; set; }
    public decimal LastAmount { get; set; }
    public string? LastCurrency { get; set; }
    public int TransferSuccessRevision { get; private set; }

    public void IncrementTransferSuccessRevision()
    {
        TransferSuccessRevision++;
        NotifyChanged();
    }

    public void NotifyChanged() => OnChange?.Invoke();

    private void ClearForNewSession()
    {
        AccountBusy = false;
        AccountBusyVersion = null;
        AccountUncertain = false;
        AccountUncertainDetail = null;
        AccountSuccess = null;
        AccountError = null;
        TransferBusy = false;
        TransferBusyVersion = null;
        TransferUncertain = false;
        TransferUncertainDetail = null;
        TransferSuccess = null;
        TransferError = null;
        LastTransferReceipt = null;
        LastSourceId = null;
        LastDestId = null;
        LastAmount = 0;
        LastCurrency = null;
        _ownerVersion = _auth.SessionVersion;
        NotifyChanged();
    }

    public void AcknowledgeAccountUncertainty()
    {
        AccountUncertain = false;
        AccountUncertainDetail = null;
        NotifyChanged();
    }

    public void AcknowledgeTransferUncertainty()
    {
        TransferUncertain = false;
        TransferUncertainDetail = null;
        NotifyChanged();
    }
}
