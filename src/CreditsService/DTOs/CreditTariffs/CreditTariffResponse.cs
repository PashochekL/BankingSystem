namespace CreditsService.DTOs.CreditTariffs;

public sealed record CreditTariffResponse(
    Guid Id,
    string Name,
    decimal InterestRate,
    bool IsActive,
    DateTimeOffset CreatedAt);
