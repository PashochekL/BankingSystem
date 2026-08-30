using AccountsService.DTOs.Accounts;
using AccountsService.Entities;
using AccountsService.Exceptions;
using AccountsService.Repositories;

namespace AccountsService.Services;

public sealed class AccountService(
    IAccountRepository accountRepository,
    ICurrentUserService currentUserService,
    ILogger<AccountService> logger) : IAccountService
{
    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken)
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

        await accountRepository.AddAsync(account, cancellationToken);

        logger.LogInformation("Account {AccountId} created for user {UserId}", account.Id, account.UserId);

        return MapToResponse(account);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var accounts = currentUserService.IsEmployee
            ? await accountRepository.GetAllAsync(cancellationToken)
            : await accountRepository.GetByUserIdAsync(currentUserId, cancellationToken);

        return accounts
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<AccountResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        return MapToResponse(account);
    }

    public async Task CloseAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        if (!account.IsClosed)
        {
            if (account.Balance != 0)
            {
                throw new ValidationException("Account balance must be zero before closing.");
            }

            account.IsClosed = true;
            account.ClosedAt = DateTimeOffset.UtcNow;

            try
            {
                await accountRepository.UpdateAsync(account, cancellationToken);
            }
            catch (ConflictException exception)
            {
                logger.LogWarning(exception, "Account {AccountId} close conflict", account.Id);
                throw;
            }

            logger.LogInformation("Account {AccountId} closed", account.Id);
        }
    }

    public async Task<AccountResponse> DepositAsync(
        Guid id,
        AccountAmountRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAmount(request.Amount);

        var account = await accountRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        if (account.IsClosed)
        {
            throw new ValidationException("Account is closed.");
        }

        account.Balance += request.Amount;

        try
        {
            await accountRepository.AddOperationAsync(new AccountOperation
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = AccountOperationType.Deposit,
                Amount = request.Amount,
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
        catch (ConflictException exception)
        {
            logger.LogWarning(exception, "Account {AccountId} deposit conflict for amount {Amount}", account.Id, request.Amount);
            throw;
        }

        logger.LogInformation("Account {AccountId} deposited by {Amount}", account.Id, request.Amount);

        return MapToResponse(account);
    }

    public async Task<AccountResponse> WithdrawAsync(
        Guid id,
        AccountAmountRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAmount(request.Amount);

        var account = await accountRepository.GetByIdForUpdateAsync(id, cancellationToken)
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

        try
        {
            await accountRepository.AddOperationAsync(new AccountOperation
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = AccountOperationType.Withdraw,
                Amount = request.Amount,
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
        catch (ConflictException exception)
        {
            logger.LogWarning(exception, "Account {AccountId} withdraw conflict for amount {Amount}", account.Id, request.Amount);
            throw;
        }

        logger.LogInformation("Account {AccountId} withdrawn by {Amount}", account.Id, request.Amount);

        return MapToResponse(account);
    }

    public async Task<IReadOnlyList<AccountOperationResponse>> GetOperationsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);

        var account = await accountRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Account was not found.");

        EnsureCanAccess(account);

        var operations = await accountRepository.GetOperationsAsync(id, page, pageSize, cancellationToken);

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
