using CreditsService.DTOs.Credits;

namespace CreditsService.Services;

public interface ICreditService
{
    Task<CreditResponse> CreateAsync(CreateCreditRequest request);
}
