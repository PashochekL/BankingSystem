using CreditsService.DTOs.CreditTariffs;
using CreditsService.Entities;
using CreditsService.Exceptions;
using CreditsService.Repositories;
using CreditsService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CreditsService.Tests.Services;

public sealed class CreditTariffServiceTests
{
    private readonly Mock<ICreditTariffRepository> creditTariffRepository = new();

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesTariff()
    {
        CreditTariff? addedTariff = null;
        creditTariffRepository
            .Setup(repository => repository.AddAsync(It.IsAny<CreditTariff>(), It.IsAny<CancellationToken>()))
            .Callback<CreditTariff, CancellationToken>((tariff, _) => addedTariff = tariff)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.CreateAsync(new CreateCreditTariffRequest(" Standard ", 12.5m, true), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Standard", response.Name);
        Assert.Equal(12.5m, response.InterestRate);
        Assert.True(response.IsActive);
        Assert.NotNull(addedTariff);
        Assert.Equal(response.Id, addedTariff.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.123)]
    public async Task CreateAsync_WithInvalidInterestRate_ThrowsValidationException(decimal interestRate)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateCreditTariffRequest("Standard", interestRate, true), CancellationToken.None));

        creditTariffRepository.Verify(
            repository => repository.AddAsync(It.IsAny<CreditTariff>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithInactiveFlag_UpdatesTariff()
    {
        var tariff = CreateTariff(isActive: true);
        creditTariffRepository
            .Setup(repository => repository.GetByIdAsync(tariff.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tariff);
        creditTariffRepository
            .Setup(repository => repository.UpdateAsync(tariff, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.UpdateAsync(tariff.Id, new UpdateCreditTariffRequest(null, null, false), CancellationToken.None);

        Assert.False(response.IsActive);
        Assert.False(tariff.IsActive);
        creditTariffRepository.Verify(
            repository => repository.UpdateAsync(tariff, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTariffMissing_ThrowsNotFoundException()
    {
        var tariffId = Guid.NewGuid();
        creditTariffRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(tariffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditTariff?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(tariffId, CancellationToken.None));
    }

    private CreditTariffService CreateService()
    {
        return new CreditTariffService(
            creditTariffRepository.Object,
            NullLogger<CreditTariffService>.Instance);
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
}
