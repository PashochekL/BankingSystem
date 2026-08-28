namespace AccountsService.Entities;

public sealed class AccountOperation
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public AccountOperationType Type { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
