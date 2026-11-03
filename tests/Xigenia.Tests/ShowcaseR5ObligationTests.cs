using Xigenia.Web.Models;
using Xigenia.Web.Services;

namespace Xigenia.Tests;

public sealed class ShowcaseR5ObligationTests
{
    [Fact]
    public void CreateObligationRequest_DoesNotExposeOwnerId()
    {
        var props = typeof(CreateObligationRequest).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("OwnerId", props);
        Assert.Contains("Kind", props);
        Assert.Contains("IssueDate", props);
        Assert.Contains("DueDate", props);
    }

    [Fact]
    public void SettlementDirection_FollowsObligationKind()
    {
        Assert.Equal(MovementDirectionDto.Pagar, DirectionFor(ObligationKindDto.PorPagar));
        Assert.Equal(MovementDirectionDto.Cobrar, DirectionFor(ObligationKindDto.PorCobrar));
    }

    [Fact]
    public void SettlementRequest_IsUnclassifiedWithoutCategory()
    {
        var obligationId = Guid.NewGuid();
        var req = new CreateMoneyMovementRequest(
            MovementDirectionDto.Pagar, 10.00m, "USD", new DateOnly(2026, 11, 3), null, obligationId, null,
            MovementClassificationDto.Unclassified, null);
        Assert.Equal(MovementClassificationDto.Unclassified, req.Classification);
        Assert.Null(req.FinancialCategoryId);
        Assert.Equal(obligationId, req.ObligationId);
    }

    [Fact]
    public void SettlementAmount_MustNotExceedOutstanding()
    {
        Assert.True(ExceedsOutstanding(10.01m, 10.00m));
        Assert.False(ExceedsOutstanding(10.00m, 10.00m));
        Assert.False(ExceedsOutstanding(4.50m, 10.00m));
    }

    [Fact]
    public void DueDate_MustNotPrecedeIssueDate()
    {
        var issue = new DateOnly(2026, 11, 3);
        Assert.True(new DateOnly(2026, 11, 2) < issue);
        Assert.False(new DateOnly(2026, 11, 3) < issue);
    }

    [Fact]
    public void ObligationMutationState_ClearsSessionOwnedStatusOnNewSession()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("token-a", "a@synthetic.invalid");
        var state = new ObligationMutationState(auth);
        state.EnsureOwner();
        state.CreateUncertain = true;
        state.CreateUncertainDetail = "detail-a";
        state.SettleUncertain = true;
        state.SettleUncertainDetail = "settle-a";

        auth.SetAuthenticated("token-b", "b@synthetic.invalid");

        Assert.False(state.CreateUncertain);
        Assert.Null(state.CreateUncertainDetail);
        Assert.False(state.SettleUncertain);
        Assert.Null(state.SettleUncertainDetail);
        Assert.Null(state.LastSettlementReceipt);
    }

    [Fact]
    public void ObligationMutationState_AcknowledgeClearsWithoutPosting()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("token-a", "a@synthetic.invalid");
        var state = new ObligationMutationState(auth);
        state.EnsureOwner();
        state.CreateUncertain = true;
        state.CreateUncertainDetail = "detail";
        state.SettleUncertain = true;
        state.SettleUncertainDetail = "detail";

        state.AcknowledgeCreateUncertainty();
        state.AcknowledgeSettleUncertainty();

        Assert.False(state.CreateUncertain);
        Assert.False(state.SettleUncertain);
    }

    [Fact]
    public void ObligationMutationState_SuccessRevisionAdvances()
    {
        var auth = new AuthState();
        auth.SetAuthenticated("token-a", "a@synthetic.invalid");
        var state = new ObligationMutationState(auth);
        state.EnsureOwner();
        var before = state.ObligationSuccessRevision;
        state.IncrementSuccessRevision();
        Assert.Equal(before + 1, state.ObligationSuccessRevision);
    }

    private static MovementDirectionDto DirectionFor(ObligationKindDto kind) =>
        kind == ObligationKindDto.PorCobrar ? MovementDirectionDto.Cobrar : MovementDirectionDto.Pagar;

    private static bool ExceedsOutstanding(decimal amount, decimal outstanding) => amount > outstanding;
}
