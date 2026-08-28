namespace AccountsService.DTOs.Accounts;

public sealed record CreateAccountRequest(
    string Name,
    Guid? UserId);
