using Xigenia.Web.Models;

namespace Xigenia.Web.Services;

public sealed class CategoryMutationState
{
    private readonly AuthState _auth;

    public event Action? OnChange;

    public CategoryMutationState(AuthState auth)
    {
        _auth = auth;
        _auth.OnChange += HandleAuthChange;
    }

    public void NotifyChanged() => OnChange?.Invoke();

    private void HandleAuthChange()
    {
        // Clear session-owned drafts and feedback synchronously on auth replacement
        if (_ownerVersion != _auth.SessionVersion)
        {
            ClearForNewSession();
        }
    }

    private int? _ownerVersion;
    public int SuccessRevision { get; private set; }

    public void IncrementSuccessRevision()
    {
        SuccessRevision++;
        NotifyChanged();
    }

    // Create
    public string CreateName { get; set; } = string.Empty;
    public MovementClassificationDto? CreateClassification { get; set; }
    public Guid? CreateParentId { get; set; }
    public bool CreateBusy { get; set; }
    public bool CreateUncertain { get; set; }
    public string? CreateUncertainDetail { get; set; }
    public string? CreateError { get; set; }
    public string? CreateSuccess { get; set; }

    // Rename per-id
    public Dictionary<Guid, string> RenameDrafts { get; } = new();
    public HashSet<Guid> RenameBusy { get; } = new();
    public Dictionary<Guid, string> RenameUncertain { get; } = new();
    public Dictionary<Guid, string> RenameErrors { get; } = new();

    // Activation per-id
    public HashSet<Guid> ActivationBusy { get; } = new();
    public Dictionary<Guid, string> ActivationUncertain { get; } = new();
    public Dictionary<Guid, string> ActivationErrors { get; } = new();
    public HashSet<Guid> PendingDeactivationConfirm { get; } = new();

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
        CreateName = string.Empty;
        CreateClassification = null;
        CreateParentId = null;
        CreateBusy = false;
        CreateUncertain = false;
        CreateUncertainDetail = null;
        CreateError = null;
        CreateSuccess = null;
        RenameDrafts.Clear();
        RenameBusy.Clear();
        RenameUncertain.Clear();
        RenameErrors.Clear();
        ActivationBusy.Clear();
        ActivationUncertain.Clear();
        ActivationErrors.Clear();
        PendingDeactivationConfirm.Clear();
        _ownerVersion = _auth.SessionVersion;
        NotifyChanged();
    }

    public void AcknowledgeCreateUncertainty()
    {
        CreateUncertain = false;
        CreateUncertainDetail = null;
    }

    public void AcknowledgeRenameUncertainty(Guid id)
    {
        RenameUncertain.Remove(id);
    }

    public void AcknowledgeActivationUncertainty(Guid id)
    {
        ActivationUncertain.Remove(id);
    }
}
