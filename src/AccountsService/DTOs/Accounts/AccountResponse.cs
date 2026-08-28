namespace AccountsService.DTOs.Accounts;

public sealed record AccountResponse(
    Guid Id,
    Guid UserId,
    string Name,
    decimal Balance,
    bool IsClosed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt);
