namespace CreditsService.DTOs.CreditTariffs;

public sealed record UpdateCreditTariffRequest(
    string? Name,
    decimal? InterestRate,
    bool? IsActive);
