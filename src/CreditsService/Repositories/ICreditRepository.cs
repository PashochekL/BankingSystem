using CreditsService.Entities;

namespace CreditsService.Repositories;

public interface ICreditRepository
{
    Task<Credit?> GetByIdAsync(Guid id);

    Task<Credit?> GetByIdForUpdateAsync(Guid id);

    Task<IReadOnlyList<Credit>> GetAllAsync();

    Task<IReadOnlyList<Credit>> GetByUserIdAsync(Guid userId);

    Task AddAsync(Credit credit, CreditOperation operation);

    Task AddOperationAsync(CreditOperation operation);

    Task<IReadOnlyList<CreditOperation>> GetOperationsAsync(Guid creditId, int page, int pageSize);
}
