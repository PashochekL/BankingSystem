using CreditsService.Entities;

namespace CreditsService.Repositories;

public interface ICreditRepository
{
    Task<Credit?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Credit?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Credit>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Credit>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Credit>> GetActiveForInterestAccrualAsync(DateTimeOffset accrualBefore);

    Task AddAsync(Credit credit, CreditOperation operation, CancellationToken cancellationToken);

    Task UpdateAsync(Credit credit, CancellationToken cancellationToken = default);

    Task AddOperationAsync(CreditOperation operation, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CreditOperation>> GetOperationsAsync(
        Guid creditId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
