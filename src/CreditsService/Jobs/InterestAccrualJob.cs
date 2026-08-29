using CreditsService.Entities;
using CreditsService.Repositories;

namespace CreditsService.Jobs;

public sealed class InterestAccrualJob(ICreditRepository creditRepository) : IInterestAccrualJob
{
    public async Task RunAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var accrualDate = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var credits = await creditRepository.GetActiveForInterestAccrualAsync(accrualDate);

        foreach (var credit in credits)
        {
            var days = (accrualDate - new DateTimeOffset(credit.LastInterestAccrualAt.UtcDateTime.Date, TimeSpan.Zero)).Days;
            if (days <= 0 || credit.RemainingAmount <= 0)
            {
                continue;
            }

            var interest = Math.Round(
                credit.RemainingAmount * credit.InterestRate / 100m * days / 365m,
                2,
                MidpointRounding.AwayFromZero);

            if (interest <= 0)
            {
                continue;
            }

            credit.RemainingAmount += interest;
            credit.LastInterestAccrualAt = accrualDate;

            await creditRepository.AddOperationAsync(new CreditOperation
            {
                Id = Guid.NewGuid(),
                CreditId = credit.Id,
                Type = CreditOperationType.InterestAccrual,
                Amount = interest,
                CreatedAt = now
            });
        }
    }
}
