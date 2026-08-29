using CreditsService.Entities;
using CreditsService.Exceptions;
using CreditsService.Repositories;
using Hangfire;

namespace CreditsService.Jobs;

public sealed class InterestAccrualJob(
    ICreditRepository creditRepository,
    ILogger<InterestAccrualJob> logger) : IInterestAccrualJob
{
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task RunAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var accrualDate = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var credits = await creditRepository.GetActiveForInterestAccrualAsync(accrualDate);

        logger.LogInformation(
            "Interest accrual started for {AccrualDate}; {CreditCount} credits selected",
            accrualDate,
            credits.Count);

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
                    logger.LogInformation("Credit {CreditId} interest accrual skipped with zero interest", credit.Id);
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

                logger.LogInformation(
                    "Credit {CreditId} interest accrued by {Amount}",
                    credit.Id,
                    interest);
            }
            catch (ConflictException exception)
            {
                logger.LogWarning(exception, "Credit {CreditId} interest accrual conflict", credit.Id);
                continue;
            }
        }

        logger.LogInformation("Interest accrual finished for {AccrualDate}", accrualDate);
    }
}
