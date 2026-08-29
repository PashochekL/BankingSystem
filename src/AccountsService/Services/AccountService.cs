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
        ValidateName(request.Name);

        var currentUserId = GetCurrentUserId();
        var accountUserId = currentUserService.IsEmployee && request.UserId.HasValue
            ? request.UserId.Value
            : currentUserId;

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = accountUserId,
            Name = request.Name.Trim(),
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
        ValidateAmount(request.Amount);

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

    public async Task<AccountResponse> WithdrawAsync(Guid id, AccountAmountRequest request)
    {
        ValidateAmount(request.Amount);

        var account = await accountRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        if (account.IsClosed)
        {
            throw new ValidationException("Account is closed.");
        }

        if (account.Balance < request.Amount)
        {
            throw new ValidationException("Insufficient account balance.");
        }

        account.Balance -= request.Amount;

        await accountRepository.AddOperationAsync(new AccountOperation
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Type = AccountOperationType.Withdraw,
            Amount = request.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return MapToResponse(account);
    }

    public async Task<IReadOnlyList<AccountOperationResponse>> GetOperationsAsync(Guid id, int page, int pageSize)
    {
        ValidatePagination(page, pageSize);

        var account = await accountRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        var operations = await accountRepository.GetOperationsAsync(id, page, pageSize);

        return operations
            .Select(MapToOperationResponse)
            .ToList();
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

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        if (amount > 9999999999999999.99m)
        {
            throw new ValidationException("Amount is too large.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ValidationException("Amount must not have more than 2 decimal places.");
        }
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page <= 0)
        {
            throw new ValidationException("Page must be greater than zero.");
        }

        if (pageSize <= 0)
        {
            throw new ValidationException("Page size must be greater than zero.");
        }

        if (pageSize > 100)
        {
            throw new ValidationException("Page size must not exceed 100.");
        }
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

    private static AccountOperationResponse MapToOperationResponse(AccountOperation operation)
    {
        return new AccountOperationResponse(
            operation.Id,
            operation.AccountId,
            operation.Type,
            operation.Amount,
            operation.CreatedAt);
    }
}
