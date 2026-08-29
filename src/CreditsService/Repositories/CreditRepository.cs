using CreditsService.Data;
using CreditsService.Entities;

namespace CreditsService.Repositories;

public sealed class CreditRepository(CreditsDbContext dbContext) : ICreditRepository
{
    public async Task AddAsync(Credit credit, CreditOperation operation)
    {
        await dbContext.Credits.AddAsync(credit);
        await dbContext.CreditOperations.AddAsync(operation);
        await dbContext.SaveChangesAsync();
    }
}
