using AccountsService.Entities;

namespace AccountsService.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(Account account, CancellationToken cancellationToken);

    Task UpdateAsync(Account account, CancellationToken cancellationToken);

    Task AddOperationAsync(AccountOperation operation, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountOperation>> GetOperationsAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
