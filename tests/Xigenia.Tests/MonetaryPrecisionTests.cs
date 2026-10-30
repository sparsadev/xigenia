using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class MonetaryPrecisionTests
{
    private static readonly Guid AccountAId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AccountBId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ObligationId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateOnly MovementDate = new(2026, 3, 14);
    private static readonly DateOnly IssueDate = new(2026, 3, 1);
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";

    // Direct Money validations

    [Theory]
    [InlineData(0)]
    [InlineData(0.10)]
    [InlineData(0.20)]
    [InlineData(10)]
    [InlineData(10.50)]
    [InlineData(99.99)]
    [InlineData(1234.56)]
    public void Money_ValidTwoDecimals_DoesNotThrow(decimal amount)
    {
        var ex = Record.Exception(() => new Money(amount, "USD"));
        Assert.Null(ex);
    }

    [Fact]
    public void Money_MaxPositiveBoundary_AcceptsExactDecimal()
    {
        var ex = Record.Exception(() => new Money(9999999999999999.99m, "USD"));
        Assert.Null(ex);
    }

    [Fact]
    public void Money_MaxNegativeBoundary_AcceptsExactDecimal()
    {
        var ex = Record.Exception(() => new Money(-9999999999999999.99m, "USD"));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0.100)]
    [InlineData(1.100)]
    [InlineData(10.000)]
    [InlineData(123.4500)]
    public void Money_TrailingZeros_DoesNotThrow(decimal amount)
    {
        var money = new Money(amount, "USD");
        Assert.Equal(amount, money.Amount);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    [InlineData(1.001)]
    [InlineData(10.999)]
    [InlineData(0.0001)]
    [InlineData(99.991)]
    public void Money_ExcessivePrecision_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, "USD"));
        Assert.Equal("amount", ex.ParamName);
    }

    [Fact]
    public void Money_ExcessivePrecision_MaxBoundaryPlusFraction_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(9999999999999999.991m, "USD"));
        Assert.Equal("amount", ex.ParamName);
    }

    public static TheoryData<decimal> MoneyRangeExceedingData => new()
    {
        10000000000000000.00m,
        10000000000000000.01m,
        -10000000000000000.00m,
        99999999999999999.99m
    };

    [Theory]
    [MemberData(nameof(MoneyRangeExceedingData))]
    public void Money_ExceedingDecimal182Range_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, "USD"));
        Assert.Equal("amount", ex.ParamName);
    }

    // FinancialAccount openingBalance

    [Theory]
    [InlineData(0)]
    [InlineData(0.10)]
    [InlineData(0.20)]
    [InlineData(-10)]
    [InlineData(-10.50)]
    public void FinancialAccount_ValidOpeningBalance_DoesNotThrow(decimal openingBalance)
    {
        var ex = Record.Exception(() => CreateAccount(openingBalance: openingBalance));
        Assert.Null(ex);
    }

    [Fact]
    public void FinancialAccount_MaxPositiveBoundaryOpeningBalance_AcceptsExactDecimal()
    {
        var ex = Record.Exception(() => CreateAccount(openingBalance: 9999999999999999.99m));
        Assert.Null(ex);
    }

    [Fact]
    public void FinancialAccount_MaxNegativeBoundaryOpeningBalance_AcceptsExactDecimal()
    {
        var ex = Record.Exception(() => CreateAccount(openingBalance: -9999999999999999.99m, type: FinancialAccountType.BankAccount));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(1.2345)]
    [InlineData(-0.001)]
    [InlineData(10.999)]
    public void FinancialAccount_ExcessivePrecisionOpeningBalance_ThrowsArgumentOutOfRangeException(decimal openingBalance)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(openingBalance: openingBalance));
        Assert.Equal("openingBalance", ex.ParamName);
    }

    [Fact]
    public void FinancialAccount_TrailingZerosOpeningBalance_Preserves()
    {
        var account = CreateAccount(openingBalance: 1.100m);
        Assert.Equal(1.100m, account.OpeningBalance);
    }

    public static TheoryData<decimal> OpeningBalanceRangeExceedingData => new()
    {
        10000000000000000.00m,
        -10000000000000000.00m
    };

    [Theory]
    [MemberData(nameof(OpeningBalanceRangeExceedingData))]
    public void FinancialAccount_RangeOpeningBalance_ThrowsArgumentOutOfRangeException(decimal openingBalance)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(openingBalance: openingBalance));
        Assert.Equal("openingBalance", ex.ParamName);
    }

    // FinancialAccount creditLimit

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.10)]
    [InlineData(100.50)]
    public void FinancialAccount_ValidCreditLimit_DoesNotThrow(decimal creditLimit)
    {
        var ex = Record.Exception(() => CreateAccount(type: FinancialAccountType.CreditCard, creditLimit: creditLimit));
        Assert.Null(ex);
    }

    [Fact]
    public void FinancialAccount_MaxPositiveBoundaryCreditLimit_AcceptsExactDecimal()
    {
        var ex = Record.Exception(() => CreateAccount(type: FinancialAccountType.CreditCard, creditLimit: 9999999999999999.99m));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(100.999)]
    [InlineData(0.123456)]
    public void FinancialAccount_ExcessivePrecisionCreditLimit_ThrowsArgumentOutOfRangeException(decimal creditLimit)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(type: FinancialAccountType.CreditCard, creditLimit: creditLimit));
        Assert.Equal("creditLimit", ex.ParamName);
    }

    public static TheoryData<decimal> CreditLimitRangeExceedingData => new()
    {
        10000000000000000.00m
    };

    [Theory]
    [MemberData(nameof(CreditLimitRangeExceedingData))]
    public void FinancialAccount_RangeCreditLimit_ThrowsArgumentOutOfRangeException(decimal creditLimit)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(type: FinancialAccountType.CreditCard, creditLimit: creditLimit));
        Assert.Equal("creditLimit", ex.ParamName);
    }

    // Regression: validation precedence – unsupported creditLimit on non-credit-card must win over precision

    [Theory]
    [InlineData(FinancialAccountType.Cash)]
    [InlineData(FinancialAccountType.BankAccount)]
    [InlineData(FinancialAccountType.DigitalWallet)]
    public void FinancialAccount_NonCreditCard_WithHighPrecisionCreditLimit_ThrowsUnsupportedFieldException(FinancialAccountType type)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(type: type, creditLimit: 0.001m));
        Assert.Equal("creditLimit", ex.ParamName);
        Assert.IsType<ArgumentException>(ex);
        Assert.NotStrictEqual(typeof(ArgumentOutOfRangeException), ex.GetType());
    }

    [Theory]
    [InlineData(FinancialAccountType.Cash)]
    [InlineData(FinancialAccountType.BankAccount)]
    public void FinancialAccount_NonCreditCard_WithRangeExceedingCreditLimit_ThrowsUnsupportedFieldException(FinancialAccountType type)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateAccount(type: type, creditLimit: 10000000000000000.00m));
        Assert.Equal("creditLimit", ex.ParamName);
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void FinancialAccount_NonCreditCard_WithExcessivePrecisionCreditLimitAndStatementDay_ThrowsCreditLimitUnsupportedFirst()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FinancialAccount(
            Guid.NewGuid(),
            OwnerId,
            "Checking",
            FinancialAccountType.Cash,
            "USD",
            0m,
            true,
            CreatedAt,
            0.001m,
            5,
            25));
        Assert.Equal("creditLimit", ex.ParamName);
    }

    // Obligation amount via Money

    [Theory]
    [InlineData(0.01)]
    [InlineData(250.75)]
    [InlineData(0.10)]
    public async Task FinanceService_CreateObligation_ValidAmount_Persists(decimal amount)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await service.CreateObligationAsync(OwnerId, new CreateObligationRequest(ObligationKind.PorPagar, "Invoice", amount, "USD", IssueDate, null), CancellationToken.None);

        Assert.Single(store.AddedObligations);
    }

    [Fact]
    public async Task FinanceService_CreateObligation_MaxPositiveBoundary_PersistsExactDecimal()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await service.CreateObligationAsync(OwnerId, new CreateObligationRequest(ObligationKind.PorPagar, "Invoice", 9999999999999999.99m, "USD", IssueDate, null), CancellationToken.None);

        Assert.Single(store.AddedObligations);
        Assert.Equal(9999999999999999.99m, store.AddedObligations[0].Total.Amount);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    [InlineData(1.001)]
    public async Task FinanceService_CreateObligation_ExcessivePrecision_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateObligationAsync(OwnerId, new CreateObligationRequest(ObligationKind.PorPagar, "Invoice", amount, "USD", IssueDate, null), CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
        Assert.Empty(store.AddedObligations);
    }

    public static TheoryData<decimal> ObligationRangeExceedingData => new()
    {
        10000000000000000.00m
    };

    [Theory]
    [MemberData(nameof(ObligationRangeExceedingData))]
    public async Task FinanceService_CreateObligation_RangeExceeded_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateObligationAsync(OwnerId, new CreateObligationRequest(ObligationKind.PorPagar, "Invoice", amount, "USD", IssueDate, null), CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
    }

    // Money movement via FinanceService

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.10)]
    [InlineData(0.20)]
    [InlineData(100.00)]
    [InlineData(1.100)]
    public async Task FinanceService_CreateMoneyMovement_ValidAmount_Persists(decimal amount)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(MovementDirection.Cobrar, amount, "USD", MovementDate, null, null, null), CancellationToken.None);

        Assert.Single(store.AddedMovements);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    [InlineData(1.2345)]
    public async Task FinanceService_CreateMoneyMovement_ExcessivePrecision_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(MovementDirection.Cobrar, amount, "USD", MovementDate, null, null, null), CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
        Assert.Empty(store.AddedMovements);
    }

    [Fact]
    public async Task FinanceService_CreateMoneyMovement_TrailingZeros_Persists()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        await service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(MovementDirection.Cobrar, 1.100m, "USD", MovementDate, null, null, null), CancellationToken.None);

        Assert.Equal(1.100m, Assert.Single(store.AddedMovements).Amount.Amount);
    }

    public static TheoryData<decimal> MovementRangeExceedingData => new()
    {
        10000000000000000.00m
    };

    [Theory]
    [MemberData(nameof(MovementRangeExceedingData))]
    public async Task FinanceService_CreateMoneyMovement_RangeExceeded_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(MovementDirection.Cobrar, amount, "USD", MovementDate, null, null, null), CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
    }

    // Obligation settlement via FinanceService (linked movement)

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.10)]
    [InlineData(50.00)]
    public async Task FinanceService_CreateSettlement_ValidAmount_Persists(decimal amount)
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationId, OwnerId, ObligationKind.PorPagar, "Invoice", new Money(100m, "USD"), IssueDate));
        store.SeedSettlementTotal(ObligationId, OwnerId, 0m);
        var service = new FinanceService(store);

        await service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(MovementDirection.Pagar, amount, "USD", MovementDate, null, ObligationId, null), CancellationToken.None);

        Assert.Single(store.LinkedMovements);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    public async Task FinanceService_CreateSettlement_ExcessivePrecision_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var store = new FakeFinanceStore();
        store.SeedObligation(new Obligation(ObligationId, OwnerId, ObligationKind.PorPagar, "Invoice", new Money(100m, "USD"), IssueDate));
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateMoneyMovementAsync(OwnerId, new CreateMoneyMovementRequest(MovementDirection.Pagar, amount, "USD", MovementDate, null, ObligationId, null), CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
        Assert.Empty(store.LinkedMovements);
    }

    // Transfer amount

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.10)]
    [InlineData(250.75)]
    [InlineData(1.100)]
    public async Task FinanceService_CreateTransfer_ValidAmount_Persists(decimal amount)
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(new FinancialAccount(AccountAId, OwnerId, "A", FinancialAccountType.BankAccount, "USD", 0m, true, CreatedAt));
        store.SeedAccount(new FinancialAccount(AccountBId, OwnerId, "B", FinancialAccountType.BankAccount, "USD", 0m, true, CreatedAt));
        var service = new FinanceService(store);

        await service.CreateTransferAsync(OwnerId, new CreateTransferRequest(AccountAId, AccountBId, amount, "USD", MovementDate, null), CancellationToken.None);

        Assert.Single(store.RecordedTransfers);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123456)]
    [InlineData(1.001)]
    public async Task FinanceService_CreateTransfer_ExcessivePrecision_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(new FinancialAccount(AccountAId, OwnerId, "A", FinancialAccountType.BankAccount, "USD", 0m, true, CreatedAt));
        store.SeedAccount(new FinancialAccount(AccountBId, OwnerId, "B", FinancialAccountType.BankAccount, "USD", 0m, true, CreatedAt));
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateTransferAsync(OwnerId, new CreateTransferRequest(AccountAId, AccountBId, amount, "USD", MovementDate, null), CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
        Assert.Empty(store.RecordedTransfers);
    }

    public static TheoryData<decimal> TransferRangeExceedingData => new()
    {
        10000000000000000.00m
    };

    [Theory]
    [MemberData(nameof(TransferRangeExceedingData))]
    public async Task FinanceService_CreateTransfer_RangeExceeded_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var store = new FakeFinanceStore();
        store.SeedAccount(new FinancialAccount(AccountAId, OwnerId, "A", FinancialAccountType.BankAccount, "USD", 0m, true, CreatedAt));
        store.SeedAccount(new FinancialAccount(AccountBId, OwnerId, "B", FinancialAccountType.BankAccount, "USD", 0m, true, CreatedAt));
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateTransferAsync(OwnerId, new CreateTransferRequest(AccountAId, AccountBId, amount, "USD", MovementDate, null), CancellationToken.None));

        Assert.Equal("amount", ex.ParamName);
    }

    // Zero handling

    [Fact]
    public void Money_Zero_Valid()
    {
        var m = new Money(0m, "USD");
        Assert.Equal(0m, m.Amount);
    }

    [Fact]
    public void Money_ZeroWithTrailingZeros_Valid()
    {
        var m = new Money(0.00m, "USD");
        Assert.Equal(0.00m, m.Amount);
        var m2 = new Money(0.000m, "USD");
        Assert.Equal(0.000m, m2.Amount);
    }

    // Negative handling according to existing rules: Money allows negative but FinancialAccount openingBalance for credit card must be non-negative
    [Fact]
    public void FinancialAccount_NegativeOpeningBalanceAsset_ValidIfTwoDecimals()
    {
        var account = CreateAccount(openingBalance: -10.50m, type: FinancialAccountType.BankAccount);
        Assert.Equal(-10.50m, account.OpeningBalance);
    }

    [Fact]
    public void FinancialAccount_NegativeCreditCardOpeningBalance_StillThrowsOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccount(type: FinancialAccountType.CreditCard, openingBalance: -0.01m));
        Assert.Equal("openingBalance", ex.ParamName);
    }

    private static FinancialAccount CreateAccount(
        decimal openingBalance = 0m,
        FinancialAccountType type = FinancialAccountType.BankAccount,
        decimal? creditLimit = null) => new(
            AccountAId,
            OwnerId,
            "Checking",
            type,
            "USD",
            openingBalance,
            true,
            CreatedAt,
            creditLimit);
}
