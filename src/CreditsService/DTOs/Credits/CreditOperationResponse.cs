using CreditsService.Entities;

namespace CreditsService.DTOs.Credits;

public sealed record CreditOperationResponse(
    Guid Id,
    Guid CreditId,
    CreditOperationType Type,
    decimal Amount,
    DateTimeOffset CreatedAt);
