using CreditsService.DTOs.Credits;

namespace CreditsService.Services;

public interface ICreditService
{
    Task<CreditResponse> CreateAsync(CreateCreditRequest request);

    Task<IReadOnlyList<CreditResponse>> GetAllAsync();

    Task<CreditResponse> GetByIdAsync(Guid id);

    Task<CreditResponse> RepayAsync(Guid id, RepayCreditRequest request);
}
