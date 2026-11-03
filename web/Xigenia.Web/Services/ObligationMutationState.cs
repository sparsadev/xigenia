using Xigenia.Web.Models;

namespace Xigenia.Web.Services;

public sealed class ObligationMutationState
{
    private readonly AuthState _auth;

    public event Action? OnChange;

    public ObligationMutationState(AuthState auth)
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
    public int ObligationSuccessRevision { get; private set; }

    public void IncrementSuccessRevision()
    {
        ObligationSuccessRevision++;
        NotifyChanged();
    }

    // Creation
    public bool CreateBusy { get; set; }
    public int? CreateBusyVersion { get; set; }
    public bool CreateUncertain { get; set; }
    public string? CreateUncertainDetail { get; set; }
    public string? CreateSuccess { get; set; }
    public string? CreateError { get; set; }

    // Settlement
    public bool SettleBusy { get; set; }
    public int? SettleBusyVersion { get; set; }
    public bool SettleUncertain { get; set; }
    public string? SettleUncertainDetail { get; set; }
    public string? SettleSuccess { get; set; }
    public string? SettleError { get; set; }
    public MoneyMovementResponse? LastSettlementReceipt { get; set; }
    public Guid? LastObligationId { get; set; }
    public decimal LastAmount { get; set; }
    public string? LastCurrency { get; set; }

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
        CreateBusy = false;
        CreateBusyVersion = null;
        CreateUncertain = false;
        CreateUncertainDetail = null;
        CreateSuccess = null;
        CreateError = null;
        SettleBusy = false;
        SettleBusyVersion = null;
        SettleUncertain = false;
        SettleUncertainDetail = null;
        SettleSuccess = null;
        SettleError = null;
        LastSettlementReceipt = null;
        LastObligationId = null;
        LastAmount = 0;
        LastCurrency = null;
        _ownerVersion = _auth.SessionVersion;
        NotifyChanged();
    }

    public void AcknowledgeCreateUncertainty()
    {
        CreateUncertain = false;
        CreateUncertainDetail = null;
    }

    public void AcknowledgeSettleUncertainty()
    {
        SettleUncertain = false;
        SettleUncertainDetail = null;
    }
}
