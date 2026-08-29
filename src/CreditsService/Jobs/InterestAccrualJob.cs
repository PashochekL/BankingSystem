using CreditsService.Entities;
using CreditsService.Exceptions;
using CreditsService.Repositories;
using Hangfire;

namespace CreditsService.Jobs;

public sealed class InterestAccrualJob(ICreditRepository creditRepository) : IInterestAccrualJob
{
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
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

            credit.LastInterestAccrualAt = accrualDate;

            try
            {
                if (interest <= 0)
                {
                    await creditRepository.UpdateAsync(credit);
                    continue;
                }

                credit.RemainingAmount += interest;

                await creditRepository.AddOperationAsync(new CreditOperation
                {
                    Id = Guid.NewGuid(),
                    CreditId = credit.Id,
                    Type = CreditOperationType.InterestAccrual,
                    Amount = interest,
                    CreatedAt = now
                });
            }
            catch (ConflictException)
            {
                continue;
            }
        }
    }
}
