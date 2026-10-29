using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class MoneyMovementDateTests
{
    [Fact]
    public void OrdinaryMovement_WithMinDate_IsRejected()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateOrdinary(DateOnly.MinValue));

        Assert.Equal("movementDate", ex.ParamName);
    }

    [Fact]
    public void OrdinaryMovement_WithValidDate_IsAccepted()
    {
        var date = new DateOnly(2026, 1, 15);

        var movement = CreateOrdinary(date);

        Assert.Equal(date, movement.MovementDate);
    }

    [Fact]
    public void TransferMovement_WithMinDate_IsRejected()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateTransferLeg(DateOnly.MinValue));

        Assert.Equal("movementDate", ex.ParamName);
    }

    [Fact]
    public void TransferMovement_WithValidDate_IsAccepted()
    {
        var date = new DateOnly(2026, 1, 15);

        var movement = CreateTransferLeg(date);

        Assert.Equal(date, movement.MovementDate);
    }

    private static MoneyMovement CreateOrdinary(DateOnly movementDate) => new(
        Guid.NewGuid(),
        "user-1",
        null,
        MovementDirection.Pagar,
        new Money(100, "USD"),
        movementDate);

    private static MoneyMovement CreateTransferLeg(DateOnly movementDate) => new(
        Guid.NewGuid(),
        "user-1",
        null,
        MovementDirection.Pagar,
        new Money(100, "USD"),
        movementDate,
        null,
        null,
        Guid.NewGuid(),
        MovementClassification.InternalTransfer);
}
