using Xigenia.Web.Models;
using Xigenia.Web.Services;

namespace Xigenia.Tests;

/// <summary>
/// Session-isolation regression coverage for the Financial insights panel state.
/// A Blazor renderer (bUnit) is not referenced by this test project, so these tests
/// exercise the panel's session-version gating directly: Home.razor delegates every
/// AI state mutation to <see cref="AiExplanationPanelState"/> and clears it on every
/// <see cref="AuthState"/> transition (logout and A-to-B replacement alike).
/// </summary>
public sealed class AiExplanationSessionTests
{
    private static FinancialExplanationResponse SampleResponse(string explanation = "Spending is steady.") =>
        new(explanation, "muse-spark-1.3", "meta",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, false, "Not authoritative.");

    private static AiExplanationPanelState WithVisibleExplanation(int version = 1)
    {
        var panel = new AiExplanationPanelState { Question = "Where did my money go?" };
        Assert.True(panel.TryBegin(out var requestVersion, version));
        panel.ApplySuccess(requestVersion, version, false, SampleResponse());
        panel.Finish(requestVersion, version, false);
        Assert.NotNull(panel.Response);
        return panel;
    }

    [Fact]
    public void Session_replacement_clears_visible_explanation()
    {
        var panel = WithVisibleExplanation(version: 1);

        // Simulates Home.HandleAuthChange on A-to-B replacement.
        panel.Clear();

        Assert.Equal(string.Empty, panel.Question);
        Assert.Null(panel.Error);
        Assert.Null(panel.Response);
        Assert.False(panel.IsBusy);
    }

    [Fact]
    public void Pending_result_from_previous_session_is_discarded()
    {
        var panel = new AiExplanationPanelState { Question = "Summarize please" };
        Assert.True(panel.TryBegin(out var requestVersion, 1));
        Assert.True(panel.IsBusy);

        // Session replaced while the request is in flight; late success must not appear.
        panel.ApplySuccess(requestVersion, currentVersion: 2, disposed: false, SampleResponse());

        Assert.Null(panel.Response);
        Assert.Null(panel.Error);
    }

    [Fact]
    public void Logout_while_pending_discards_result_and_releases_guard_on_transition()
    {
        var panel = new AiExplanationPanelState { Question = "Summarize please" };
        Assert.True(panel.TryBegin(out var requestVersion, 1));

        // Simulates logout transition clearing superseded state.
        panel.Clear();

        panel.ApplySuccess(requestVersion, currentVersion: 2, disposed: false, SampleResponse());
        Assert.Null(panel.Response);
        Assert.False(panel.IsBusy);
    }

    [Fact]
    public void Late_exception_after_logout_is_discarded()
    {
        var panel = new AiExplanationPanelState { Question = "Summarize please" };
        Assert.True(panel.TryBegin(out var requestVersion, 1));

        panel.ApplyError(requestVersion, currentVersion: 1, disposed: true, "boom");

        Assert.Null(panel.Error);
    }

    [Fact]
    public void Late_exception_after_session_replacement_is_discarded()
    {
        var panel = new AiExplanationPanelState { Question = "Summarize please" };
        Assert.True(panel.TryBegin(out var requestVersion, 1));

        panel.ApplyError(requestVersion, currentVersion: 2, disposed: false, "boom");

        Assert.Null(panel.Error);
    }

    [Fact]
    public void Same_session_success_applies_and_releases_guard()
    {
        var panel = new AiExplanationPanelState { Question = "Summarize please" };
        Assert.True(panel.TryBegin(out var requestVersion, 7));

        panel.ApplySuccess(requestVersion, currentVersion: 7, disposed: false, SampleResponse("Steady."));
        panel.Finish(requestVersion, currentVersion: 7, disposed: false);

        Assert.Equal("Steady.", panel.Response!.Explanation);
        Assert.Null(panel.Error);
        Assert.False(panel.IsBusy);
    }

    [Fact]
    public void Same_session_error_applies_and_releases_guard()
    {
        var panel = new AiExplanationPanelState { Question = "Summarize please" };
        Assert.True(panel.TryBegin(out var requestVersion, 7));

        panel.ApplyError(requestVersion, currentVersion: 7, disposed: false, "Session expired. Please sign in again.");
        panel.Finish(requestVersion, currentVersion: 7, disposed: false);

        Assert.Null(panel.Response);
        Assert.Contains("Session expired", panel.Error);
        Assert.False(panel.IsBusy);
    }

    [Fact]
    public void Superseded_completion_does_not_release_newer_request_guard()
    {
        var panel = new AiExplanationPanelState { Question = "First question here?" };
        Assert.True(panel.TryBegin(out var firstVersion, 1));

        // A-to-B transition clears the superseded request, then B starts its own.
        panel.Clear();
        panel.Question = "Second question here?";
        Assert.True(panel.TryBegin(out var secondVersion, 2));
        Assert.True(panel.IsBusy);

        // Late completion of A's request must neither show A's data nor free B's guard.
        panel.ApplySuccess(firstVersion, currentVersion: 2, disposed: false, SampleResponse("A's answer"));
        panel.Finish(firstVersion, currentVersion: 2, disposed: false);

        Assert.Null(panel.Response);
        Assert.True(panel.IsBusy);

        // B's own completion applies normally.
        panel.ApplySuccess(secondVersion, currentVersion: 2, disposed: false, SampleResponse("B's answer"));
        panel.Finish(secondVersion, currentVersion: 2, disposed: false);
        Assert.Equal("B's answer", panel.Response!.Explanation);
        Assert.False(panel.IsBusy);
    }

    [Fact]
    public void Begin_is_blocked_while_busy_and_rejects_short_question()
    {
        var panel = new AiExplanationPanelState { Question = "Summarize my spending please" };
        Assert.True(panel.TryBegin(out _, 3));
        Assert.False(panel.TryBegin(out _, 3));

        var shortPanel = new AiExplanationPanelState { Question = "hi" };
        Assert.False(shortPanel.TryBegin(out _, 3));
        Assert.Contains("at least 3 characters", shortPanel.Error);
        Assert.False(shortPanel.IsBusy);
    }

    [Fact]
    public void Empty_success_sets_empty_state_error_not_response()
    {
        var panel = new AiExplanationPanelState { Question = "Summarize my spending please" };
        Assert.True(panel.TryBegin(out var requestVersion, 4));

        panel.ApplySuccess(requestVersion, currentVersion: 4, disposed: false, SampleResponse("   "));
        panel.Finish(requestVersion, currentVersion: 4, disposed: false);

        Assert.Null(panel.Response);
        Assert.Contains("empty response", panel.Error);
        Assert.False(panel.IsBusy);
    }
}
