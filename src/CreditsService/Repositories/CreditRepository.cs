using CreditsService.Data;
using CreditsService.Entities;
using CreditsService.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CreditsService.Repositories;

public sealed class CreditRepository(CreditsDbContext dbContext) : ICreditRepository
{
    public async Task<Credit?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Credits
            .AsNoTracking()
            .FirstOrDefaultAsync(credit => credit.Id == id, cancellationToken);
    }

    public async Task<Credit?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Credits
            .FirstOrDefaultAsync(credit => credit.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Credit>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Credits
            .AsNoTracking()
            .OrderBy(credit => credit.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credit>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Credits
            .AsNoTracking()
            .Where(credit => credit.UserId == userId)
            .OrderBy(credit => credit.CreatedAt)
            .ToListAsync(cancellationToken);
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

    public async Task AddAsync(Credit credit, CreditOperation operation, CancellationToken cancellationToken)
    {
        await dbContext.Credits.AddAsync(credit, cancellationToken);
        await dbContext.CreditOperations.AddAsync(operation, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Credit credit, CancellationToken cancellationToken = default)
    {
        dbContext.Credits.Update(credit);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task AddOperationAsync(CreditOperation operation, CancellationToken cancellationToken = default)
    {
        await dbContext.CreditOperations.AddAsync(operation, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CreditOperation>> GetOperationsAsync(
        Guid creditId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return await dbContext.CreditOperations
            .AsNoTracking()
            .Where(operation => operation.CreditId == creditId)
            .OrderByDescending(operation => operation.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException("Credit was changed by another operation. Try again.", exception);
        }
    }
}
