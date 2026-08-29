using CreditsService.DTOs.Credits;
using CreditsService.Entities;
using CreditsService.Exceptions;
using CreditsService.Repositories;

namespace CreditsService.Services;

public sealed class CreditService(
    ICreditRepository creditRepository,
    ICreditTariffRepository creditTariffRepository,
    ICurrentUserService currentUserService) : ICreditService
{
    public async Task<CreditResponse> CreateAsync(CreateCreditRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        var currentUserId = GetCurrentUserId();
        var tariff = await creditTariffRepository.GetByIdAsync(request.TariffId)
            ?? throw new NotFoundException("Credit tariff was not found.");

        if (!tariff.IsActive)
        {
            throw new ValidationException("Credit tariff is not active.");
        }

        var createdAt = DateTimeOffset.UtcNow;
        var credit = new Credit
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            TariffId = tariff.Id,
            InitialAmount = request.Amount,
            RemainingAmount = request.Amount,
            InterestRate = tariff.InterestRate,
            CreatedAt = createdAt,
            LastInterestAccrualAt = createdAt,
            Status = CreditStatus.Active
        };

        var operation = new CreditOperation
        {
            Id = Guid.NewGuid(),
            CreditId = credit.Id,
            Type = CreditOperationType.Creation,
            Amount = request.Amount,
            CreatedAt = createdAt
        };

        await creditRepository.AddAsync(credit, operation);

        return MapToResponse(credit);
    }

    public async Task<IReadOnlyList<CreditResponse>> GetAllAsync()
    {
        var currentUserId = GetCurrentUserId();
        var credits = currentUserService.IsEmployee
            ? await creditRepository.GetAllAsync()
            : await creditRepository.GetByUserIdAsync(currentUserId);

        return credits
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<CreditResponse> GetByIdAsync(Guid id)
    {
        var credit = await creditRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Credit was not found.");

        EnsureCanAccess(credit);

        return MapToResponse(credit);
    }

    public async Task<CreditResponse> RepayAsync(Guid id, RepayCreditRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        var credit = await creditRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("Credit was not found.");

        EnsureCanAccess(credit);

        if (credit.Status != CreditStatus.Active)
        {
            throw new ValidationException("Credit is not active.");
        }

        if (request.Amount > credit.RemainingAmount)
        {
            throw new ValidationException("Repayment amount must not exceed remaining amount.");
        }

        credit.RemainingAmount -= request.Amount;

        if (credit.RemainingAmount == 0)
        {
            credit.Status = CreditStatus.Paid;
        }

        await creditRepository.AddOperationAsync(new CreditOperation
        {
            Id = Guid.NewGuid(),
            CreditId = credit.Id,
            Type = CreditOperationType.Repayment,
            Amount = request.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return MapToResponse(credit);
    }

    public async Task<IReadOnlyList<CreditOperationResponse>> GetOperationsAsync(Guid id, int page, int pageSize)
    {
        if (page <= 0)
        {
            throw new ValidationException("Page must be greater than zero.");
        }

        if (pageSize <= 0)
        {
            throw new ValidationException("Page size must be greater than zero.");
        }

        var credit = await creditRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Credit was not found.");

        EnsureCanAccess(credit);

        var operations = await creditRepository.GetOperationsAsync(id, page, pageSize);

        return operations
            .Select(MapToOperationResponse)
            .ToList();
    }

    private Guid GetCurrentUserId()
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not { } userId)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        return userId;
    }

    private void EnsureCanAccess(Credit credit)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserService.IsEmployee && credit.UserId != currentUserId)
        {
            throw new ForbiddenException("Credit access is forbidden.");
        }
    }

    private static CreditResponse MapToResponse(Credit credit)
    {
        return new CreditResponse(
            credit.Id,
            credit.UserId,
            credit.TariffId,
            credit.InitialAmount,
            credit.RemainingAmount,
            credit.InterestRate,
            credit.CreatedAt,
            credit.LastInterestAccrualAt,
            credit.Status);
    }

    private static CreditOperationResponse MapToOperationResponse(CreditOperation operation)
    {
        return new CreditOperationResponse(
            operation.Id,
            operation.CreditId,
            operation.Type,
            operation.Amount,
            operation.CreatedAt);
    }
}
