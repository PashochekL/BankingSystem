namespace CreditsService.DTOs.CreditTariffs;

public sealed record CreateCreditTariffRequest(
    string Name,
    decimal InterestRate,
    bool IsActive);
