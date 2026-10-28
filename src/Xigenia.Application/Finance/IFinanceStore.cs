using Xigenia.Domain;

namespace Xigenia.Application.Finance;

public interface IFinanceStore
{
    Task AddObligationAsync(Obligation obligation, CancellationToken cancellationToken);

    Task<IReadOnlyList<Obligation>> ListObligationsAsync(string ownerId, CancellationToken cancellationToken);
}
