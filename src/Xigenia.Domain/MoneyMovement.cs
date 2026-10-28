namespace Xigenia.Domain;

public sealed class MoneyMovement
{
    private MoneyMovement()
    {
        // EF Core materialization only.
        OwnerId = null!;
        Amount = null!;
    }

    public MoneyMovement(
        Guid id,
        string ownerId,
        Guid? obligationId,
        MovementDirection direction,
        Money amount,
        DateOnly movementDate,
        string? note = null,
        Guid? financialAccountId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        if (obligationId == Guid.Empty)
        {
            throw new ArgumentException("ObligationId cannot be empty.", nameof(obligationId));
        }

        if (financialAccountId == Guid.Empty)
        {
            throw new ArgumentException("FinancialAccountId cannot be empty.", nameof(financialAccountId));
        }

        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        ArgumentNullException.ThrowIfNull(amount);

        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Movement amount must be positive.");
        }

        Id = id;
        OwnerId = ownerId;
        ObligationId = obligationId;
        FinancialAccountId = financialAccountId;
        Direction = direction;
        Amount = amount;
        MovementDate = movementDate;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid Id { get; private set; }

    public string OwnerId { get; private set; }

    public Guid? ObligationId { get; private set; }

    public Guid? FinancialAccountId { get; private set; }

    public MovementDirection Direction { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly MovementDate { get; private set; }

    public string? Note { get; private set; }
}
