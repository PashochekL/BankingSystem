namespace CreditsService.DTOs.Credits;

public sealed record CreateCreditRequest(
    Guid TariffId,
    decimal Amount);
