using CreditsService.Data;
using CreditsService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditsService.Repositories;

public sealed class CreditTariffRepository(CreditsDbContext dbContext) : ICreditTariffRepository
{
    public async Task<CreditTariff?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.CreditTariffs
            .AsNoTracking()
            .FirstOrDefaultAsync(tariff => tariff.Id == id, cancellationToken);
    }

    public async Task<CreditTariff?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.CreditTariffs
            .FirstOrDefaultAsync(tariff => tariff.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CreditTariff>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.CreditTariffs
            .AsNoTracking()
            .OrderBy(tariff => tariff.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CreditTariff tariff, CancellationToken cancellationToken)
    {
        await dbContext.CreditTariffs.AddAsync(tariff, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CreditTariff tariff, CancellationToken cancellationToken)
    {
        dbContext.CreditTariffs.Update(tariff);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
