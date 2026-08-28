using AccountsService.DTOs.Accounts;
using AccountsService.Entities;
using AccountsService.Exceptions;
using AccountsService.Repositories;

namespace AccountsService.Services;

public sealed class AccountService(
    IAccountRepository accountRepository,
    ICurrentUserService currentUserService) : IAccountService
{
    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var accountUserId = currentUserService.IsEmployee && request.UserId.HasValue
            ? request.UserId.Value
            : currentUserId;

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = accountUserId,
            Name = request.Name,
            Balance = 0,
            IsClosed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await accountRepository.AddAsync(account);

        return MapToResponse(account);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync()
    {
        var currentUserId = GetCurrentUserId();
        var accounts = currentUserService.IsEmployee
            ? await accountRepository.GetAllAsync()
            : await accountRepository.GetByUserIdAsync(currentUserId);

        return accounts
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<AccountResponse> GetByIdAsync(Guid id)
    {
        var account = await accountRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        return MapToResponse(account);
    }

    public async Task CloseAsync(Guid id)
    {
        var account = await accountRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        if (!account.IsClosed)
        {
            account.IsClosed = true;
            account.ClosedAt = DateTimeOffset.UtcNow;
            await accountRepository.UpdateAsync(account);
        }
    }

    public async Task<AccountResponse> DepositAsync(Guid id, AccountAmountRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        var account = await accountRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        if (account.IsClosed)
        {
            throw new ValidationException("Account is closed.");
        }

        account.Balance += request.Amount;

        await accountRepository.AddOperationAsync(new AccountOperation
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Type = AccountOperationType.Deposit,
            Amount = request.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return MapToResponse(account);
    }

    private Guid GetCurrentUserId()
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not { } userId)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        return userId;
    }

    private void EnsureCanAccess(Account account)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserService.IsEmployee && account.UserId != currentUserId)
        {
            throw new ForbiddenException("Account access is forbidden.");
        }
    }

    private static AccountResponse MapToResponse(Account account)
    {
        return new AccountResponse(
            account.Id,
            account.UserId,
            account.Name,
            account.Balance,
            account.IsClosed,
            account.CreatedAt,
            account.ClosedAt);
    }
}
