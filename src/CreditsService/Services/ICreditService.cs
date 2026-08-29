using CreditsService.DTOs.Credits;

namespace CreditsService.Services;

public interface ICreditService
{
    Task<CreditResponse> CreateAsync(CreateCreditRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<CreditResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreditResponse> RepayAsync(Guid id, RepayCreditRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditOperationResponse>> GetOperationsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
