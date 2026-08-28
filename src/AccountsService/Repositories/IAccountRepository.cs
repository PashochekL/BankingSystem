using AccountsService.Entities;

namespace AccountsService.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id);

    Task<Account?> GetByIdForUpdateAsync(Guid id);

    Task<IReadOnlyList<Account>> GetAllAsync();

    Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId);

    Task AddAsync(Account account);

    Task UpdateAsync(Account account);

    Task AddOperationAsync(AccountOperation operation);

    Task<IReadOnlyList<AccountOperation>> GetOperationsAsync(Guid accountId, int page, int pageSize);
}
