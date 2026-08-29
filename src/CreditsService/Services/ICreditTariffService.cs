using CreditsService.DTOs.CreditTariffs;

namespace CreditsService.Services;

public interface ICreditTariffService
{
    Task<CreditTariffResponse> CreateAsync(CreateCreditTariffRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditTariffResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<CreditTariffResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreditTariffResponse> UpdateAsync(
        Guid id,
        UpdateCreditTariffRequest request,
        CancellationToken cancellationToken);
}
