using CreditsService.Entities;
using CreditsService.Jobs;
using CreditsService.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CreditsService.Tests.Jobs;

public sealed class InterestAccrualJobTests
{
    private readonly Mock<ICreditRepository> creditRepository = new();

    [Fact]
    public async Task RunAsync_WithEligibleCredit_AccruesInterestAndAddsOperation()
    {
        var accrualDate = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        var credit = CreateCredit(remainingAmount: 1000m, interestRate: 36.5m, lastAccrualAt: accrualDate.AddDays(-10));
        CreditOperation? addedOperation = null;

        creditRepository
            .Setup(repository => repository.GetActiveForInterestAccrualAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new[] { credit });
        creditRepository
            .Setup(repository => repository.AddOperationAsync(It.IsAny<CreditOperation>(), It.IsAny<CancellationToken>()))
            .Callback<CreditOperation, CancellationToken>((operation, _) => addedOperation = operation)
            .Returns(Task.CompletedTask);

        var job = CreateJob();

        await job.RunAsync();

        Assert.Equal(1010m, credit.RemainingAmount);
        Assert.Equal(accrualDate, credit.LastInterestAccrualAt);
        Assert.NotNull(addedOperation);
        Assert.Equal(credit.Id, addedOperation.CreditId);
        Assert.Equal(CreditOperationType.InterestAccrual, addedOperation.Type);
        Assert.Equal(10m, addedOperation.Amount);
    }

    [Fact]
    public async Task RunAsync_WithAlreadyAccruedCredit_DoesNotAddOperation()
    {
        var accrualDate = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        var credit = CreateCredit(remainingAmount: 1000m, interestRate: 36.5m, lastAccrualAt: accrualDate);

        creditRepository
            .Setup(repository => repository.GetActiveForInterestAccrualAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new[] { credit });

        var job = CreateJob();

        await job.RunAsync();

        Assert.Equal(1000m, credit.RemainingAmount);
        creditRepository.Verify(
            repository => repository.AddOperationAsync(It.IsAny<CreditOperation>(), It.IsAny<CancellationToken>()),
            Times.Never);
        creditRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Credit>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WithZeroInterest_UpdatesLastAccrualWithoutOperation()
    {
        var accrualDate = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        var credit = CreateCredit(remainingAmount: 0.01m, interestRate: 0.01m, lastAccrualAt: accrualDate.AddDays(-1));

        creditRepository
            .Setup(repository => repository.GetActiveForInterestAccrualAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new[] { credit });
        creditRepository
            .Setup(repository => repository.UpdateAsync(credit, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var job = CreateJob();

        await job.RunAsync();

        Assert.Equal(0.01m, credit.RemainingAmount);
        Assert.Equal(accrualDate, credit.LastInterestAccrualAt);
        creditRepository.Verify(
            repository => repository.UpdateAsync(credit, It.IsAny<CancellationToken>()),
            Times.Once);
        creditRepository.Verify(
            repository => repository.AddOperationAsync(It.IsAny<CreditOperation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private InterestAccrualJob CreateJob()
    {
        return new InterestAccrualJob(
            creditRepository.Object,
            NullLogger<InterestAccrualJob>.Instance);
    }

    private static Credit CreateCredit(
        decimal remainingAmount,
        decimal interestRate,
        DateTimeOffset lastAccrualAt)
    {
        return new Credit
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TariffId = Guid.NewGuid(),
            InitialAmount = 1000m,
            RemainingAmount = remainingAmount,
            InterestRate = interestRate,
            CreatedAt = lastAccrualAt.AddDays(-30),
            LastInterestAccrualAt = lastAccrualAt,
            Status = CreditStatus.Active
        };
    }
}
