using Xigenia.Web.Models;

namespace Xigenia.Web.Services;

/// <summary>
/// Session-safe state holder for the Financial insights panel.
/// Every state mutation caused by an asynchronous outcome is gated on the
/// session version captured when the request started, so results belonging
/// to a previous session (or a disposed component) can never become visible.
/// </summary>
public sealed class AiExplanationPanelState
{
    public string Question { get; set; } = string.Empty;
    public string? Error { get; private set; }
    public FinancialExplanationResponse? Response { get; private set; }
    public bool IsBusy { get; private set; }

    /// <summary>Clears all AI-sensitive state. Called on every session transition (logout or A-to-B replacement).</summary>
    public void Clear()
    {
        Question = string.Empty;
        Error = null;
        Response = null;
        IsBusy = false;
    }

    public void SelectSuggestion(string suggestion)
    {
        Question = suggestion;
        Error = null;
    }

    /// <summary>Attempts to start a request. Returns false when busy or the question is invalid.</summary>
    public bool TryBegin(out int requestVersion, int sessionVersion)
    {
        requestVersion = sessionVersion;
        if (IsBusy) return false;
        if (string.IsNullOrWhiteSpace(Question) || Question.Trim().Length < 3)
        {
            Error = "Enter a question of at least 3 characters.";
            return false;
        }
        Error = null;
        Response = null;
        IsBusy = true;
        return true;
    }

    /// <summary>Applies a successful response unless it belongs to a previous session or a disposed component.</summary>
    public void ApplySuccess(int requestVersion, int currentVersion, bool disposed, FinancialExplanationResponse? data)
    {
        if (disposed || requestVersion != currentVersion) return;
        if (data is null || string.IsNullOrWhiteSpace(data.Explanation))
        {
            Error = "The AI returned an empty response. Try again or rephrase your question.";
        }
        else
        {
            Response = data;
        }
    }

    /// <summary>Applies an error unless it belongs to a previous session or a disposed component.</summary>
    public void ApplyError(int requestVersion, int currentVersion, bool disposed, string detail)
    {
        if (disposed || requestVersion != currentVersion) return;
        Error = detail;
    }

    /// <summary>Releases the submit guard, but only for the request that is still current.</summary>
    public void Finish(int requestVersion, int currentVersion, bool disposed)
    {
        if (!disposed && requestVersion == currentVersion) IsBusy = false;
    }
}
