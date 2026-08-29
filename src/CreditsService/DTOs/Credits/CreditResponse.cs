using CreditsService.Entities;

namespace CreditsService.DTOs.Credits;

public sealed record CreditResponse(
    Guid Id,
    Guid UserId,
    Guid TariffId,
    decimal InitialAmount,
    decimal RemainingAmount,
    decimal InterestRate,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastInterestAccrualAt,
    CreditStatus Status);
