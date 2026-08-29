namespace CreditsService.Entities;

public sealed class CreditOperation
{
    public Guid Id { get; set; }

    public Guid CreditId { get; set; }

    public Credit Credit { get; set; } = null!;

    public CreditOperationType Type { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
