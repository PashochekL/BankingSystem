using CreditsService.Data;
using CreditsService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditsService.Repositories;

public sealed class CreditRepository(CreditsDbContext dbContext) : ICreditRepository
{
    public async Task<Credit?> GetByIdAsync(Guid id)
    {
        return await dbContext.Credits
            .AsNoTracking()
            .FirstOrDefaultAsync(credit => credit.Id == id);
    }

    public async Task<IReadOnlyList<Credit>> GetAllAsync()
    {
        return await dbContext.Credits
            .AsNoTracking()
            .OrderBy(credit => credit.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Credit>> GetByUserIdAsync(Guid userId)
    {
        return await dbContext.Credits
            .AsNoTracking()
            .Where(credit => credit.UserId == userId)
            .OrderBy(credit => credit.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Credit credit, CreditOperation operation)
    {
        await dbContext.Credits.AddAsync(credit);
        await dbContext.CreditOperations.AddAsync(operation);
        await dbContext.SaveChangesAsync();
    }
}
