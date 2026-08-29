namespace CreditsService.Entities;

public sealed class Credit
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TariffId { get; set; }

    public CreditTariff Tariff { get; set; } = null!;

    public decimal InitialAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public decimal InterestRate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastInterestAccrualAt { get; set; }

    public CreditStatus Status { get; set; }

    public List<CreditOperation> Operations { get; set; } = [];
}
