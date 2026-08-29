using CreditsService.Entities;

namespace CreditsService.Repositories;

public interface ICreditTariffRepository
{
    Task<CreditTariff?> GetByIdAsync(Guid id);

    Task<CreditTariff?> GetByIdForUpdateAsync(Guid id);

    Task<IReadOnlyList<CreditTariff>> GetAllAsync();

    Task AddAsync(CreditTariff tariff);

    Task UpdateAsync(CreditTariff tariff);
}
