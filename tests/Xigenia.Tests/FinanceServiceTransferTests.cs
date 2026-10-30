using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceTransferTests
{
    private static readonly Guid SourceAccountId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DestinationAccountId = new("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateOnly MovementDate = new(2026, 4, 5);
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    [Fact]
    public async Task CreateTransfer_PersistsPairedLegsWithConsistentValues()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId, type: FinancialAccountType.BankAccount));
        store.SeedAccount(CreateAccount(DestinationAccountId, type: FinancialAccountType.DigitalWallet));
        var service = new FinanceService(store);

        await service.CreateTransferAsync(OwnerId, CreateTransferRequest(), CancellationToken.None);

        var transfer = Assert.Single(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
        Assert.Equal(MovementDirection.Pagar, transfer.SourceLeg.Direction);
        Assert.Equal(MovementDirection.Cobrar, transfer.DestinationLeg.Direction);
        Assert.Equal(MovementClassification.InternalTransfer, transfer.SourceLeg.Classification);
        Assert.Equal(MovementClassification.InternalTransfer, transfer.DestinationLeg.Classification);
        Assert.Equal(0.123456m, transfer.SourceLeg.Amount.Amount);
        Assert.Equal(0.123456m, transfer.DestinationLeg.Amount.Amount);
        Assert.Equal("USD", transfer.SourceLeg.Amount.Currency);
        Assert.Equal("USD", transfer.DestinationLeg.Amount.Currency);
        Assert.Equal(MovementDate, transfer.SourceLeg.MovementDate);
        Assert.Equal(MovementDate, transfer.DestinationLeg.MovementDate);
        Assert.Equal("Monthly savings", transfer.SourceLeg.Note);
        Assert.Equal("Monthly savings", transfer.DestinationLeg.Note);
        Assert.Equal(OwnerId, transfer.SourceLeg.OwnerId);
        Assert.Equal(OwnerId, transfer.DestinationLeg.OwnerId);
        Assert.NotNull(transfer.SourceLeg.TransferGroupId);
        Assert.NotEqual(Guid.Empty, transfer.SourceLeg.TransferGroupId.Value);
        Assert.Equal(transfer.SourceLeg.TransferGroupId, transfer.DestinationLeg.TransferGroupId);
        Assert.NotEqual(Guid.Empty, transfer.SourceLeg.Id);
        Assert.NotEqual(Guid.Empty, transfer.DestinationLeg.Id);
        Assert.NotEqual(transfer.SourceLeg.Id, transfer.DestinationLeg.Id);
        Assert.Equal(SourceAccountId, transfer.SourceLeg.FinancialAccountId);
        Assert.Equal(DestinationAccountId, transfer.DestinationLeg.FinancialAccountId);
        Assert.Null(transfer.SourceLeg.ObligationId);
        Assert.Null(transfer.DestinationLeg.ObligationId);
        Assert.Null(transfer.SourceLeg.FinancialCategoryId);
        Assert.Null(transfer.DestinationLeg.FinancialCategoryId);
    }

    [Fact]
    public async Task CreateTransfer_ResponseMatchesCapturedLegs()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId, type: FinancialAccountType.BankAccount));
        store.SeedAccount(CreateAccount(DestinationAccountId, type: FinancialAccountType.DigitalWallet));
        var service = new FinanceService(store);

        var response = await service.CreateTransferAsync(OwnerId, CreateTransferRequest(), CancellationToken.None);

        var transfer = Assert.Single(store.RecordedTransfers);
        Assert.Equal(transfer.SourceLeg.TransferGroupId, response.TransferGroupId);
        AssertLegResponseMatches(response.SourceLeg, transfer.SourceLeg);
        AssertLegResponseMatches(response.DestinationLeg, transfer.DestinationLeg);
    }

    [Fact]
    public async Task CreateTransfer_WithEmptySourceId_ThrowsArgumentException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        var request = CreateTransferRequest() with { SourceAccountId = Guid.Empty };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTransferAsync(
            OwnerId,
            request,
            CancellationToken.None));

        Assert.Equal("request", ex.ParamName);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithEmptyDestinationId_ThrowsArgumentException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        var request = CreateTransferRequest() with { DestinationAccountId = Guid.Empty };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTransferAsync(
            OwnerId,
            request,
            CancellationToken.None));

        Assert.Equal("request", ex.ParamName);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithIdenticalAccountIds_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        var request = CreateTransferRequest() with { DestinationAccountId = SourceAccountId };

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateTransferAsync(
            OwnerId,
            request,
            CancellationToken.None));

        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithMissingSource_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(DestinationAccountId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        var lookup = Assert.Single(store.AccountLookups);
        Assert.Equal(SourceAccountId, lookup.AccountId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithMissingDestination_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        Assert.Equal(2, store.AccountLookups.Count);
        Assert.Equal((SourceAccountId, OwnerId), store.AccountLookups[0]);
        Assert.Equal((DestinationAccountId, OwnerId), store.AccountLookups[1]);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithOtherOwnerSource_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId, ownerId: OtherOwnerId));
        store.SeedAccount(CreateAccount(DestinationAccountId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        var lookup = Assert.Single(store.AccountLookups);
        Assert.Equal(SourceAccountId, lookup.AccountId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithOtherOwnerDestination_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId));
        store.SeedAccount(CreateAccount(DestinationAccountId, ownerId: OtherOwnerId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        Assert.Equal(2, store.AccountLookups.Count);
        Assert.Equal((SourceAccountId, OwnerId), store.AccountLookups[0]);
        Assert.Equal((DestinationAccountId, OwnerId), store.AccountLookups[1]);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithCreditCardSource_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId, type: FinancialAccountType.CreditCard));
        store.SeedAccount(CreateAccount(DestinationAccountId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithInactiveSource_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId, isActive: false));
        store.SeedAccount(CreateAccount(DestinationAccountId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithInactiveDestination_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId));
        store.SeedAccount(CreateAccount(DestinationAccountId, isActive: false));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithSourceCurrencyMismatch_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId, currency: "EUR"));
        store.SeedAccount(CreateAccount(DestinationAccountId));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

    [Fact]
    public async Task CreateTransfer_WithDestinationCurrencyMismatch_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(CreateAccount(SourceAccountId));
        store.SeedAccount(CreateAccount(DestinationAccountId, currency: "EUR"));
        var service = new FinanceService(store);

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateTransferAsync(
            OwnerId,
            CreateTransferRequest(),
            CancellationToken.None));

        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.LinkedMovements);
    }

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

    private static CreateTransferRequest CreateTransferRequest() => new(
        SourceAccountId: SourceAccountId,
        DestinationAccountId: DestinationAccountId,
        Amount: 0.123456m,
        Currency: "USD",
        MovementDate: MovementDate,
        Note: "Monthly savings");

    private static FinancialAccount CreateAccount(
        Guid id,
        string ownerId = OwnerId,
        FinancialAccountType type = FinancialAccountType.BankAccount,
        string currency = "USD",
        bool isActive = true) => new(
            id,
            ownerId,
            "Account",
            type,
            currency,
            0m,
            isActive,
            CreatedAt);
}
