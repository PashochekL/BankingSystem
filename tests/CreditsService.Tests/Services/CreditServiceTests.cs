using CreditsService.DTOs.Credits;
using CreditsService.Entities;
using CreditsService.Exceptions;
using CreditsService.Repositories;
using CreditsService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CreditsService.Tests.Services;

public sealed class CreditServiceTests
{
    private readonly Guid currentUserId = Guid.NewGuid();
    private readonly Mock<ICreditRepository> creditRepository = new();
    private readonly Mock<ICreditTariffRepository> creditTariffRepository = new();
    private readonly Mock<ICurrentUserService> currentUserService = new();

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesCreditAndCreationOperation()
    {
        ConfigureCurrentUser(currentUserId);
        var tariff = CreateTariff(isActive: true);
        Credit? addedCredit = null;
        CreditOperation? addedOperation = null;

        creditTariffRepository
            .Setup(repository => repository.GetByIdAsync(tariff.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tariff);
        creditRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Credit>(), It.IsAny<CreditOperation>(), It.IsAny<CancellationToken>()))
            .Callback<Credit, CreditOperation, CancellationToken>((credit, operation, _) =>
            {
                addedCredit = credit;
                addedOperation = operation;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.CreateAsync(new CreateCreditRequest(tariff.Id, 1000m), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(currentUserId, response.UserId);
        Assert.Equal(tariff.Id, response.TariffId);
        Assert.Equal(1000m, response.InitialAmount);
        Assert.Equal(1000m, response.RemainingAmount);
        Assert.Equal(tariff.InterestRate, response.InterestRate);
        Assert.Equal(CreditStatus.Active, response.Status);
        Assert.NotNull(addedCredit);
        Assert.NotNull(addedOperation);
        Assert.Equal(response.Id, addedOperation.CreditId);
        Assert.Equal(CreditOperationType.Creation, addedOperation.Type);
        Assert.Equal(1000m, addedOperation.Amount);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveTariff_ThrowsValidationException()
    {
        ConfigureCurrentUser(currentUserId);
        var tariff = CreateTariff(isActive: false);
        creditTariffRepository
            .Setup(repository => repository.GetByIdAsync(tariff.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tariff);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateCreditRequest(tariff.Id, 1000m), CancellationToken.None));

        creditRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Credit>(), It.IsAny<CreditOperation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(10.123)]
    public async Task CreateAsync_WithInvalidAmount_ThrowsValidationException(decimal amount)
    {
        ConfigureCurrentUser(currentUserId);
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateCreditRequest(Guid.NewGuid(), amount), CancellationToken.None));

        creditTariffRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenTariffMissing_ThrowsNotFoundException()
    {
        ConfigureCurrentUser(currentUserId);
        var tariffId = Guid.NewGuid();
        creditTariffRepository
            .Setup(repository => repository.GetByIdAsync(tariffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditTariff?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(new CreateCreditRequest(tariffId, 1000m), CancellationToken.None));
    }

    [Fact]
    public async Task RepayAsync_WithValidRequest_ReducesRemainingAmountAndAddsOperation()
    {
        ConfigureCurrentUser(currentUserId);
        var credit = CreateCredit(currentUserId, remainingAmount: 1000m);
        CreditOperation? addedOperation = null;

        creditRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(credit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credit);
        creditRepository
            .Setup(repository => repository.AddOperationAsync(It.IsAny<CreditOperation>(), It.IsAny<CancellationToken>()))
            .Callback<CreditOperation, CancellationToken>((operation, _) => addedOperation = operation)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.RepayAsync(credit.Id, new RepayCreditRequest(300m), CancellationToken.None);

        Assert.Equal(700m, response.RemainingAmount);
        Assert.Equal(CreditStatus.Active, response.Status);
        Assert.NotNull(addedOperation);
        Assert.Equal(credit.Id, addedOperation.CreditId);
        Assert.Equal(CreditOperationType.Repayment, addedOperation.Type);
        Assert.Equal(300m, addedOperation.Amount);
    }

    [Fact]
    public async Task RepayAsync_WithFullRepayment_MarksCreditPaid()
    {
        ConfigureCurrentUser(currentUserId);
        var credit = CreateCredit(currentUserId, remainingAmount: 500m);
        creditRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(credit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credit);
        creditRepository
            .Setup(repository => repository.AddOperationAsync(It.IsAny<CreditOperation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.RepayAsync(credit.Id, new RepayCreditRequest(500m), CancellationToken.None);

        Assert.Equal(0m, response.RemainingAmount);
        Assert.Equal(CreditStatus.Paid, response.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(10.123)]
    public async Task RepayAsync_WithInvalidAmount_ThrowsValidationException(decimal amount)
    {
        ConfigureCurrentUser(currentUserId);
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RepayAsync(Guid.NewGuid(), new RepayCreditRequest(amount), CancellationToken.None));

        creditRepository.Verify(
            repository => repository.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RepayAsync_WithAmountGreaterThanRemaining_ThrowsValidationException()
    {
        ConfigureCurrentUser(currentUserId);
        var credit = CreateCredit(currentUserId, remainingAmount: 100m);
        creditRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(credit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credit);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RepayAsync(credit.Id, new RepayCreditRequest(101m), CancellationToken.None));

        creditRepository.Verify(
            repository => repository.AddOperationAsync(It.IsAny<CreditOperation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RepayAsync_WithPaidCredit_ThrowsValidationException()
    {
        ConfigureCurrentUser(currentUserId);
        var credit = CreateCredit(currentUserId, remainingAmount: 0m, status: CreditStatus.Paid);
        creditRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(credit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credit);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RepayAsync(credit.Id, new RepayCreditRequest(10m), CancellationToken.None));
    }

    [Fact]
    public async Task RepayAsync_WithForeignCredit_ThrowsForbiddenException()
    {
        ConfigureCurrentUser(currentUserId);
        var credit = CreateCredit(Guid.NewGuid(), remainingAmount: 100m);
        creditRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(credit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credit);

        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.RepayAsync(credit.Id, new RepayCreditRequest(10m), CancellationToken.None));
    }

    [Fact]
    public async Task RepayAsync_WhenCreditMissing_ThrowsNotFoundException()
    {
        ConfigureCurrentUser(currentUserId);
        var creditId = Guid.NewGuid();
        creditRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(creditId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credit?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.RepayAsync(creditId, new RepayCreditRequest(10m), CancellationToken.None));
    }

    private CreditService CreateService()
    {
        return new CreditService(
            creditRepository.Object,
            creditTariffRepository.Object,
            currentUserService.Object,
            NullLogger<CreditService>.Instance);
    }

    private void ConfigureCurrentUser(Guid userId, bool isEmployee = false)
    {
        currentUserService.Setup(service => service.UserId).Returns(userId);
        currentUserService.Setup(service => service.Role).Returns(isEmployee ? "Employee" : "Client");
        currentUserService.Setup(service => service.IsAuthenticated).Returns(true);
        currentUserService.Setup(service => service.IsEmployee).Returns(isEmployee);
    }

    private static CreditTariff CreateTariff(bool isActive)
    {
        return new CreditTariff
        {
            Id = Guid.NewGuid(),
            Name = "Standard",
            InterestRate = 12.5m,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    private static Credit CreateCredit(
        Guid userId,
        decimal remainingAmount,
        CreditStatus status = CreditStatus.Active)
    {
        return new Credit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = Guid.NewGuid(),
            InitialAmount = 1000m,
            RemainingAmount = remainingAmount,
            InterestRate = 12.5m,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            LastInterestAccrualAt = DateTimeOffset.UtcNow.AddDays(-1),
            Status = status
        };
    }
}
