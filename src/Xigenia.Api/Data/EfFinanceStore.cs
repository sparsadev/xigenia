using Microsoft.EntityFrameworkCore;
using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Api.Data;

public sealed class EfFinanceStore(ApplicationDbContext context) : IFinanceStore
{
    public async Task AddObligationAsync(Obligation obligation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(obligation);

        context.Obligations.Add(obligation);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Obligation>> ListObligationsAsync(string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.Obligations
            .AsNoTracking()
            .Where(o => o.OwnerId == ownerId)
            .OrderBy(o => o.IssueDate)
            .ThenBy(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMoneyMovementAsync(MoneyMovement movement, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(movement);

        context.MoneyMovements.Add(movement);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MoneyMovement>> ListMoneyMovementsAsync(string ownerId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        return await context.MoneyMovements
            .AsNoTracking()
            .Where(m => m.OwnerId == ownerId)
            .OrderBy(m => m.MovementDate)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }
}
