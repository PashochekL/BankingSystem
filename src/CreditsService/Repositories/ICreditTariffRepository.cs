using CreditsService.Entities;

namespace CreditsService.Repositories;

public interface ICreditTariffRepository
{
    Task<CreditTariff?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreditTariff?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditTariff>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(CreditTariff tariff, CancellationToken cancellationToken);

    Task UpdateAsync(CreditTariff tariff, CancellationToken cancellationToken);
}
