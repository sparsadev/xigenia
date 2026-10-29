using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xigenia.Domain;

namespace Xigenia.Api.Data;

public sealed class GlobalCategoryCatalogInitializer(ApplicationDbContext context)
{
    private const string CatalogLockResource = "Xigenia:GlobalCategoryCatalog:v1";
    private const int CatalogLockTimeoutMs = 30_000;

    private sealed record ChildSeed(Guid Id, string Name);

    private sealed record ParentSeed(Guid Id, string Name, MovementClassification Classification, ChildSeed[] Children);

    private sealed record FlatSeed(Guid Id, Guid? ParentId, string Name, MovementClassification Classification);

    // Two levels by construction: children are names only and cannot nest.
    // Children inherit the parent classification by construction.
    private static readonly ParentSeed[] Catalog =
    [
        new(new Guid("c5010000-0000-0000-0000-000000000001"), "Ingresos laborales", MovementClassification.Income,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000004"), "Salario"),
            new(new Guid("c5010000-0000-0000-0000-000000000005"), "Honorarios profesionales")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000002"), "Ingresos comerciales", MovementClassification.Income,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000006"), "Ventas"),
            new(new Guid("c5010000-0000-0000-0000-000000000007"), "Comisiones")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000003"), "Otros ingresos", MovementClassification.Income,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000008"), "Regalos recibidos"),
            new(new Guid("c5010000-0000-0000-0000-000000000009"), "Otros ingresos")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000010"), "Alimentación", MovementClassification.Expense,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000017"), "Supermercado"),
            new(new Guid("c5010000-0000-0000-0000-000000000018"), "Restaurantes")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000011"), "Transporte", MovementClassification.Expense,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000019"), "Combustible"),
            new(new Guid("c5010000-0000-0000-0000-000000000020"), "Transporte público")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000012"), "Vivienda", MovementClassification.Expense,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000021"), "Alquiler"),
            new(new Guid("c5010000-0000-0000-0000-000000000022"), "Servicios básicos")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000013"), "Salud", MovementClassification.Expense,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000023"), "Consultas médicas"),
            new(new Guid("c5010000-0000-0000-0000-000000000024"), "Medicamentos")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000014"), "Educación", MovementClassification.Expense,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000025"), "Matrícula"),
            new(new Guid("c5010000-0000-0000-0000-000000000026"), "Material educativo")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000015"), "Servicios financieros", MovementClassification.Expense,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000027"), "Intereses"),
            new(new Guid("c5010000-0000-0000-0000-000000000028"), "Comisiones bancarias")
        ]),
        new(new Guid("c5010000-0000-0000-0000-000000000016"), "Otros gastos", MovementClassification.Expense,
        [
            new(new Guid("c5010000-0000-0000-0000-000000000029"), "Gastos diversos")
        ])
    ];

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Fast path: single read, no writes when the catalog is complete.
        var missing = await FindMissingAsync(cancellationToken);
        if (missing.Count == 0)
        {
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Serialize concurrent startups at the database level; the lock is
        // owned by the transaction and released on commit or rollback.
        await AcquireCatalogLockAsync(cancellationToken);

        // Re-check under the lock: a concurrent instance may have finished first.
        missing = await FindMissingAsync(cancellationToken);
        if (missing.Count == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var seed in missing)
        {
            context.FinancialCategories.Add(new FinancialCategory(
                seed.Id,
                ownerId: null,
                seed.ParentId,
                seed.Name,
                seed.Classification,
                isActive: true,
                now));
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<FlatSeed>> FindMissingAsync(CancellationToken cancellationToken)
    {
        var all = Flatten();
        var ids = all.Select(s => s.Id).ToList();
        var existing = await context.FinancialCategories
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var existingIds = existing.ToHashSet();
        return all.Where(s => !existingIds.Contains(s.Id)).ToList();
    }

    private static IReadOnlyList<FlatSeed> Flatten()
    {
        // Parents first so a child never references a missing parent.
        var flat = new List<FlatSeed>();
        foreach (var parent in Catalog)
        {
            flat.Add(new FlatSeed(parent.Id, null, parent.Name, parent.Classification));
        }

        foreach (var parent in Catalog)
        {
            foreach (var child in parent.Children)
            {
                flat.Add(new FlatSeed(child.Id, parent.Id, child.Name, parent.Classification));
            }
        }

        return flat;
    }

    private async Task AcquireCatalogLockAsync(CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        var resource = command.CreateParameter();
        resource.ParameterName = "@resource";
        resource.Value = CatalogLockResource;
        command.Parameters.Add(resource);

        command.CommandText =
            "DECLARE @lockResult INT; " +
            $"EXEC @lockResult = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {CatalogLockTimeoutMs}; " +
            "SELECT @lockResult;";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || Convert.ToInt32(result) < 0)
        {
            throw new TimeoutException(
                $"Could not acquire the '{CatalogLockResource}' database lock (result: {result ?? "null"}).");
        }
    }
}
