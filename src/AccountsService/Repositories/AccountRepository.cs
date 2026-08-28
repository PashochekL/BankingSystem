using AccountsService.Data;
using AccountsService.Entities;
using AccountsService.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountsService.Repositories;

public sealed class AccountRepository(AccountsDbContext dbContext) : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Id == id);
    }

    public async Task<Account?> GetByIdForUpdateAsync(Guid id)
    {
        return await dbContext.Accounts
            .FirstOrDefaultAsync(account => account.Id == id);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync()
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .OrderBy(account => account.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Account account)
    {
        await dbContext.Accounts.AddAsync(account);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(Account account)
    {
        dbContext.Accounts.Update(account);
        await SaveChangesAsync();
    }

    public async Task AddOperationAsync(AccountOperation operation)
    {
        await dbContext.AccountOperations.AddAsync(operation);
        await SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AccountOperation>> GetOperationsAsync(Guid accountId, int page, int pageSize)
    {
        return await dbContext.AccountOperations
            .AsNoTracking()
            .Where(operation => operation.AccountId == accountId)
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
            throw new ConflictException("Account was changed by another operation. Try again.", exception);
        }
    }
}
