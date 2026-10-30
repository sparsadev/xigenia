using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceTransferValidationTests
{
    private static readonly Guid SourceAccountId = new("15151515-1515-1515-1515-151515151515");
    private static readonly Guid DestinationAccountId = new("16161616-1616-1616-1616-161616161616");
    private static readonly DateOnly MovementDate = new(2026, 4, 5);
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateTransfer_WithNonPositiveAmount_ThrowsArgumentOutOfRangeException(int amount)
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId));
        store.SeedAccount(CreateAccount(DestinationAccountId));
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateRequest(amount: amount),
            CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateTransfer_WithMinimumDate_ThrowsArgumentOutOfRangeException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId));
        store.SeedAccount(CreateAccount(DestinationAccountId));
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateRequest(movementDate: DateOnly.MinValue),
            CancellationToken.None));

        Assert.Equal("movementDate", ex.ParamName);
        AssertNoPersistence(store);
    }

    [Fact]
    public async Task CreateTransfer_WithCreditCardDestination_PersistsPairedLegs()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId, type: FinancialAccountType.BankAccount));
        store.SeedAccount(CreateAccount(DestinationAccountId, type: FinancialAccountType.CreditCard));
        var service = new FinanceService(store);

        await service.CreateTransferAsync(OwnerId, CreateRequest(amount: 250.75m), CancellationToken.None);

        var transfer = Assert.Single(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
        Assert.Equal(MovementDirection.Pagar, transfer.SourceLeg.Direction);
        Assert.Equal(MovementDirection.Cobrar, transfer.DestinationLeg.Direction);
        Assert.Equal(MovementClassification.InternalTransfer, transfer.SourceLeg.Classification);
        Assert.Equal(MovementClassification.InternalTransfer, transfer.DestinationLeg.Classification);
        Assert.Equal(250.75m, transfer.SourceLeg.Amount.Amount);
        Assert.Equal(250.75m, transfer.DestinationLeg.Amount.Amount);
        Assert.Equal(OwnerId, transfer.SourceLeg.OwnerId);
        Assert.Equal(OwnerId, transfer.DestinationLeg.OwnerId);
        Assert.NotNull(transfer.SourceLeg.TransferGroupId);
        Assert.Equal(transfer.SourceLeg.TransferGroupId, transfer.DestinationLeg.TransferGroupId);
        Assert.Equal(SourceAccountId, transfer.SourceLeg.FinancialAccountId);
        Assert.Equal(DestinationAccountId, transfer.DestinationLeg.FinancialAccountId);
    }

    [Fact]
    public async Task CreateTransfer_WithCreditCardDestination_ResponseMatchesCapturedLegs()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId, type: FinancialAccountType.BankAccount));
        store.SeedAccount(CreateAccount(DestinationAccountId, type: FinancialAccountType.CreditCard));
        var service = new FinanceService(store);

        var response = await service.CreateTransferAsync(OwnerId, CreateRequest(amount: 250.75m), CancellationToken.None);

        var transfer = Assert.Single(store.RecordedTransfers);
        Assert.Equal(transfer.SourceLeg.TransferGroupId, response.TransferGroupId);
        AssertLegResponseMatches(response.SourceLeg, transfer.SourceLeg);
        AssertLegResponseMatches(response.DestinationLeg, transfer.DestinationLeg);
    }

    private static CreateTransferRequest CreateRequest(
        decimal amount = 100m,
        DateOnly? movementDate = null,
        string? note = "Card payment") => new(
            SourceAccountId: SourceAccountId,
            DestinationAccountId: DestinationAccountId,
            Amount: amount,
            Currency: "USD",
            MovementDate: movementDate ?? MovementDate,
            Note: note);

    private static FinancialAccount CreateAccount(
        Guid id,
        FinancialAccountType type = FinancialAccountType.BankAccount,
        string currency = "USD",
        bool isActive = true) => new(
            id,
            OwnerId,
            "Account",
            type,
            currency,
            0m,
            isActive,
            CreatedAt);

    private static void AssertLegResponseMatches(MoneyMovementResponse response, MoneyMovement captured)
    {
        Assert.Equal(captured.Id, response.Id);
        Assert.Equal(captured.FinancialAccountId, response.FinancialAccountId);
        Assert.Equal(captured.Direction, response.Direction);
        Assert.Equal(captured.Classification, response.Classification);
        Assert.Equal(captured.Amount.Amount, response.Amount);
        Assert.Equal(captured.Amount.Currency, response.Currency);
        Assert.Equal(captured.MovementDate, response.MovementDate);
        Assert.Equal(captured.Note, response.Note);
        Assert.Equal(captured.TransferGroupId, response.TransferGroupId);
        Assert.Equal(captured.ObligationId, response.ObligationId);
        Assert.Equal(captured.FinancialCategoryId, response.FinancialCategoryId);
    }

    private static void AssertNoPersistence(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
    }
}
