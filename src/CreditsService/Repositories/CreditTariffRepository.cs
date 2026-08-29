using CreditsService.Data;
using CreditsService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditsService.Repositories;

public sealed class CreditTariffRepository(CreditsDbContext dbContext) : ICreditTariffRepository
{
    public async Task<CreditTariff?> GetByIdAsync(Guid id)
    {
        return await dbContext.CreditTariffs
            .AsNoTracking()
            .FirstOrDefaultAsync(tariff => tariff.Id == id);
    }

    public async Task<CreditTariff?> GetByIdForUpdateAsync(Guid id)
    {
        return await dbContext.CreditTariffs
            .FirstOrDefaultAsync(tariff => tariff.Id == id);
    }

    public async Task<IReadOnlyList<CreditTariff>> GetAllAsync()
    {
        return await dbContext.CreditTariffs
            .AsNoTracking()
            .OrderBy(tariff => tariff.Name)
            .ToListAsync();
    }

    public async Task AddAsync(CreditTariff tariff)
    {
        await dbContext.CreditTariffs.AddAsync(tariff);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(CreditTariff tariff)
    {
        dbContext.CreditTariffs.Update(tariff);
        await dbContext.SaveChangesAsync();
    }
}
