using CreditsService.DTOs.CreditTariffs;

namespace CreditsService.Services;

public interface ICreditTariffService
{
    Task<CreditTariffResponse> CreateAsync(CreateCreditTariffRequest request);

    Task<IReadOnlyList<CreditTariffResponse>> GetAllAsync();

    Task<CreditTariffResponse> GetByIdAsync(Guid id);

    Task<CreditTariffResponse> UpdateAsync(Guid id, UpdateCreditTariffRequest request);
}
