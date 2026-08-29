using CreditsService.DTOs.CreditTariffs;
using CreditsService.Entities;
using CreditsService.Exceptions;
using CreditsService.Repositories;

namespace CreditsService.Services;

public sealed class CreditTariffService(ICreditTariffRepository creditTariffRepository) : ICreditTariffService
{
    public async Task<CreditTariffResponse> CreateAsync(CreateCreditTariffRequest request)
    {
        ValidateName(request.Name);
        ValidateInterestRate(request.InterestRate);

        var tariff = new CreditTariff
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            InterestRate = request.InterestRate,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await creditTariffRepository.AddAsync(tariff);

        return MapToResponse(tariff);
    }

    public async Task<IReadOnlyList<CreditTariffResponse>> GetAllAsync()
    {
        var tariffs = await creditTariffRepository.GetAllAsync();

        return tariffs
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<CreditTariffResponse> GetByIdAsync(Guid id)
    {
        var tariff = await creditTariffRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("Credit tariff was not found.");

        return MapToResponse(tariff);
    }

    public async Task<CreditTariffResponse> UpdateAsync(Guid id, UpdateCreditTariffRequest request)
    {
        var tariff = await creditTariffRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Credit tariff was not found.");

        if (request.Name is not null)
        {
            ValidateName(request.Name);
            tariff.Name = request.Name.Trim();
        }

        if (request.InterestRate is not null)
        {
            ValidateInterestRate(request.InterestRate.Value);
            tariff.InterestRate = request.InterestRate.Value;
        }

        if (request.IsActive is not null)
        {
            tariff.IsActive = request.IsActive.Value;
        }

        await creditTariffRepository.UpdateAsync(tariff);

        return MapToResponse(tariff);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Name is required.");
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length > 100)
        {
            throw new ValidationException("Name must not exceed 100 characters.");
        }
    }

    private static void ValidateInterestRate(decimal interestRate)
    {
        if (interestRate <= 0)
        {
            throw new ValidationException("Interest rate must be greater than zero.");
        }

        if (interestRate > 999.99m)
        {
            throw new ValidationException("Interest rate must not exceed 999.99.");
        }

        if (decimal.Round(interestRate, 2) != interestRate)
        {
            throw new ValidationException("Interest rate must not have more than 2 decimal places.");
        }
    }

    private static CreditTariffResponse MapToResponse(CreditTariff tariff)
    {
        return new CreditTariffResponse(
            tariff.Id,
            tariff.Name,
            tariff.InterestRate,
            tariff.IsActive,
            tariff.CreatedAt);
    }
}
