using CreditsService.Data;
using CreditsService.Entities;
using CreditsService.Exceptions;
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

    public async Task<Credit?> GetByIdForUpdateAsync(Guid id)
    {
        return await dbContext.Credits
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

    public async Task<IReadOnlyList<Credit>> GetActiveForInterestAccrualAsync(DateTimeOffset accrualBefore)
    {
        return await dbContext.Credits
            .Where(credit =>
                credit.Status == CreditStatus.Active &&
                credit.LastInterestAccrualAt < accrualBefore)
            .OrderBy(credit => credit.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Credit credit, CreditOperation operation)
    {
        await dbContext.Credits.AddAsync(credit);
        await dbContext.CreditOperations.AddAsync(operation);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(Credit credit)
    {
        dbContext.Credits.Update(credit);
        await SaveChangesAsync();
    }

    public async Task AddOperationAsync(CreditOperation operation)
    {
        await dbContext.CreditOperations.AddAsync(operation);
        await SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CreditOperation>> GetOperationsAsync(Guid creditId, int page, int pageSize)
    {
        return await dbContext.CreditOperations
            .AsNoTracking()
            .Where(operation => operation.CreditId == creditId)
            .OrderByDescending(operation => operation.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException("Credit was changed by another operation. Try again.", exception);
        }
    }
}
