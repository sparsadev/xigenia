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
        Guid? financialAccountId = null,
        Guid? transferGroupId = null,
        MovementClassification classification = MovementClassification.Unclassified,
        Guid? financialCategoryId = null)
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

        if (transferGroupId == Guid.Empty)
        {
            throw new ArgumentException("TransferGroupId cannot be empty.", nameof(transferGroupId));
        }

        if (financialCategoryId == Guid.Empty)
        {
            throw new ArgumentException("FinancialCategoryId cannot be empty.", nameof(financialCategoryId));
        }

        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        if (!Enum.IsDefined(classification))
        {
            throw new ArgumentOutOfRangeException(nameof(classification));
        }

        ArgumentNullException.ThrowIfNull(amount);

        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Movement amount must be positive.");
        }

        if (transferGroupId.HasValue && classification != MovementClassification.InternalTransfer)
        {
            throw new ArgumentException("Transfer movements must use InternalTransfer classification.", nameof(classification));
        }

        if (!transferGroupId.HasValue && classification == MovementClassification.InternalTransfer)
        {
            throw new ArgumentException("InternalTransfer classification requires a transfer group.", nameof(classification));
        }

        if (financialCategoryId.HasValue
            && classification != MovementClassification.Income
            && classification != MovementClassification.Expense)
        {
            throw new ArgumentException("Only Income and Expense movements may have a category.", nameof(financialCategoryId));
        }

        if (movementDate == DateOnly.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(movementDate), "Movement date is required.");
        }

        Id = id;
        OwnerId = ownerId;
        ObligationId = obligationId;
        FinancialAccountId = financialAccountId;
        TransferGroupId = transferGroupId;
        FinancialCategoryId = financialCategoryId;
        Classification = classification;
        Direction = direction;
        Amount = amount;
        MovementDate = movementDate;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid Id { get; private set; }

    public string OwnerId { get; private set; }

    public Guid? ObligationId { get; private set; }

    public Guid? FinancialAccountId { get; private set; }

    public Guid? TransferGroupId { get; private set; }

    public Guid? FinancialCategoryId { get; private set; }

    public MovementClassification Classification { get; private set; }

    public MovementDirection Direction { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly MovementDate { get; private set; }

    public string? Note { get; private set; }
}
