namespace Xigenia.Domain;

public sealed class FinancialCategory
{
    private FinancialCategory()
    {
        // EF Core materialization only.
        Name = null!;
    }

    public FinancialCategory(
        Guid id,
        string? ownerId,
        Guid? parentCategoryId,
        string name,
        MovementClassification classification,
        bool isActive,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        if (ownerId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        }

        if (parentCategoryId == Guid.Empty)
        {
            throw new ArgumentException("ParentCategoryId cannot be empty.", nameof(parentCategoryId));
        }

        if (parentCategoryId == id)
        {
            throw new ArgumentException("Category cannot be its own parent.", nameof(parentCategoryId));
        }

        name = NormalizeName(name);

        if (classification != MovementClassification.Income
            && classification != MovementClassification.Expense)
        {
            throw new ArgumentOutOfRangeException(
                nameof(classification),
                "Category classification must be Income or Expense.");
        }

        Id = id;
        OwnerId = ownerId;
        ParentCategoryId = parentCategoryId;
        Name = name;
        Classification = classification;
        IsActive = isActive;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string? OwnerId { get; private set; }

    public Guid? ParentCategoryId { get; private set; }

    public string Name { get; private set; }

    public MovementClassification Classification { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Renames the category. Authorization, including preventing
    /// modifications to global categories, must be enforced by the application layer.
    /// </summary>
    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
