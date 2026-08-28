using AccountsService.Entities;

namespace AccountsService.DTOs.Accounts;

public sealed record AccountOperationResponse(
    Guid Id,
    Guid AccountId,
    AccountOperationType Type,
    decimal Amount,
    DateTimeOffset CreatedAt);
