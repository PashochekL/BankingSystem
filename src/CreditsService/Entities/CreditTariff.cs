namespace CreditsService.Entities;

public sealed class CreditTariff
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal InterestRate { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<Credit> Credits { get; set; } = [];
}
