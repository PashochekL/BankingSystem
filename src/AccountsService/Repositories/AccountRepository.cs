using AccountsService.Data;
using AccountsService.Entities;
using AccountsService.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountsService.Repositories;

public sealed class AccountRepository(AccountsDbContext dbContext) : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Id == id, cancellationToken);
    }

    public async Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .FirstOrDefaultAsync(account => account.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .OrderBy(account => account.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        await dbContext.Accounts.AddAsync(account, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken)
    {
        dbContext.Accounts.Update(account);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task AddOperationAsync(AccountOperation operation, CancellationToken cancellationToken)
    {
        await dbContext.AccountOperations.AddAsync(operation, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountOperation>> GetOperationsAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return await dbContext.AccountOperations
            .AsNoTracking()
            .Where(operation => operation.AccountId == accountId)
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
            throw new ConflictException("Account was changed by another operation. Try again.", exception);
        }
    }
}
