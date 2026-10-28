namespace Xigenia.Domain;

public sealed class Obligation
{
    private Obligation()
    {
        // EF Core materialization only.
        OwnerId = null!;
        Title = null!;
        Total = null!;
    }

    public Obligation(
        Guid id,
        string ownerId,
        ObligationKind kind,
        string title,
        Money total,
        DateOnly issueDate,
        DateOnly? dueDate = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(total);

        if (total.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Obligation total must be positive.");
        }

        if (dueDate < issueDate)
        {
            throw new ArgumentException("DueDate cannot precede IssueDate.", nameof(dueDate));
        }

        Id = id;
        OwnerId = ownerId;
        Kind = kind;
        Title = title.Trim();
        Total = total;
        IssueDate = issueDate;
        DueDate = dueDate;
    }

    public Guid Id { get; private set; }

    public string OwnerId { get; private set; }

    public ObligationKind Kind { get; private set; }

    public string Title { get; private set; }

    public Money Total { get; private set; }

    public DateOnly IssueDate { get; private set; }

    public DateOnly? DueDate { get; private set; }
}
