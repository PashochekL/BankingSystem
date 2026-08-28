namespace AccountsService.Entities;

public sealed class Account
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public bool IsClosed { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public List<AccountOperation> Operations { get; set; } = [];
}
