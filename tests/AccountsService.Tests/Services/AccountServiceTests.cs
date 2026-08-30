using AccountsService.DTOs.Accounts;
using AccountsService.Entities;
using AccountsService.Exceptions;
using AccountsService.Repositories;
using AccountsService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AccountsService.Tests.Services;

public sealed class AccountServiceTests
{
    private readonly Guid currentUserId = Guid.NewGuid();
    private readonly Mock<IAccountRepository> accountRepository = new();
    private readonly Mock<ICurrentUserService> currentUserService = new();

    [Fact]
    public async Task CreateAsync_WithAuthenticatedClient_CreatesAccountForCurrentUser()
    {
        ConfigureCurrentUser(currentUserId);
        var request = new CreateAccountRequest(" Main account ", Guid.NewGuid());
        Account? addedAccount = null;

        accountRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((account, _) => addedAccount = account)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(currentUserId, response.UserId);
        Assert.Equal("Main account", response.Name);
        Assert.Equal(0, response.Balance);
        Assert.False(response.IsClosed);
        Assert.NotNull(addedAccount);
        Assert.Equal(response.Id, addedAccount.Id);
        Assert.Equal(currentUserId, addedAccount.UserId);
    }

    [Fact]
    public async Task CreateAsync_WithEmployeeAndUserId_CreatesAccountForRequestedUser()
    {
        var requestedUserId = Guid.NewGuid();
        ConfigureCurrentUser(currentUserId, isEmployee: true);
        Account? addedAccount = null;

        accountRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((account, _) => addedAccount = account)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.CreateAsync(new CreateAccountRequest("Client account", requestedUserId), CancellationToken.None);

        Assert.Equal(requestedUserId, response.UserId);
        Assert.NotNull(addedAccount);
        Assert.Equal(requestedUserId, addedAccount.UserId);
    }

    [Fact]
    public async Task CreateAsync_WithBlankName_ThrowsValidationException()
    {
        ConfigureCurrentUser(currentUserId);
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateAccountRequest(" ", null), CancellationToken.None));

        accountRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        ConfigureAnonymousUser();
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.CreateAsync(new CreateAccountRequest("Main account", null), CancellationToken.None));
    }

    [Fact]
    public async Task DepositAsync_WithValidRequest_AddsAmountAndOperation()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(currentUserId, balance: 100m);
        AccountOperation? addedOperation = null;

        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accountRepository
            .Setup(repository => repository.AddOperationAsync(It.IsAny<AccountOperation>(), It.IsAny<CancellationToken>()))
            .Callback<AccountOperation, CancellationToken>((operation, _) => addedOperation = operation)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.DepositAsync(account.Id, new AccountAmountRequest(25.50m), CancellationToken.None);

        Assert.Equal(125.50m, response.Balance);
        Assert.NotNull(addedOperation);
        Assert.Equal(account.Id, addedOperation.AccountId);
        Assert.Equal(AccountOperationType.Deposit, addedOperation.Type);
        Assert.Equal(25.50m, addedOperation.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(10.123)]
    public async Task DepositAsync_WithInvalidAmount_ThrowsValidationException(decimal amount)
    {
        ConfigureCurrentUser(currentUserId);
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DepositAsync(Guid.NewGuid(), new AccountAmountRequest(amount), CancellationToken.None));

        accountRepository.Verify(
            repository => repository.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DepositAsync_WhenAccountClosed_ThrowsValidationException()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(currentUserId, isClosed: true);
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DepositAsync(account.Id, new AccountAmountRequest(10m), CancellationToken.None));
    }

    [Fact]
    public async Task DepositAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        ConfigureCurrentUser(currentUserId);
        var accountId = Guid.NewGuid();
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DepositAsync(accountId, new AccountAmountRequest(10m), CancellationToken.None));
    }

    [Fact]
    public async Task DepositAsync_WithForeignAccount_ThrowsForbiddenException()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(Guid.NewGuid());
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.DepositAsync(account.Id, new AccountAmountRequest(10m), CancellationToken.None));
    }

    [Fact]
    public async Task WithdrawAsync_WithValidRequest_SubtractsAmountAndAddsOperation()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(currentUserId, balance: 100m);
        AccountOperation? addedOperation = null;

        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accountRepository
            .Setup(repository => repository.AddOperationAsync(It.IsAny<AccountOperation>(), It.IsAny<CancellationToken>()))
            .Callback<AccountOperation, CancellationToken>((operation, _) => addedOperation = operation)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.WithdrawAsync(account.Id, new AccountAmountRequest(30m), CancellationToken.None);

        Assert.Equal(70m, response.Balance);
        Assert.NotNull(addedOperation);
        Assert.Equal(account.Id, addedOperation.AccountId);
        Assert.Equal(AccountOperationType.Withdraw, addedOperation.Type);
        Assert.Equal(30m, addedOperation.Amount);
    }

    [Fact]
    public async Task WithdrawAsync_WithInsufficientBalance_ThrowsValidationException()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(currentUserId, balance: 20m);
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.WithdrawAsync(account.Id, new AccountAmountRequest(30m), CancellationToken.None));

        accountRepository.Verify(
            repository => repository.AddOperationAsync(It.IsAny<AccountOperation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(10.123)]
    public async Task WithdrawAsync_WithInvalidAmount_ThrowsValidationException(decimal amount)
    {
        ConfigureCurrentUser(currentUserId);
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.WithdrawAsync(Guid.NewGuid(), new AccountAmountRequest(amount), CancellationToken.None));

        accountRepository.Verify(
            repository => repository.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_WhenAccountClosed_ThrowsValidationException()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(currentUserId, balance: 100m, isClosed: true);
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.WithdrawAsync(account.Id, new AccountAmountRequest(10m), CancellationToken.None));
    }

    [Fact]
    public async Task WithdrawAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        ConfigureCurrentUser(currentUserId);
        var accountId = Guid.NewGuid();
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.WithdrawAsync(accountId, new AccountAmountRequest(10m), CancellationToken.None));
    }

    [Fact]
    public async Task WithdrawAsync_WithForeignAccount_ThrowsForbiddenException()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(Guid.NewGuid(), balance: 100m);
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.WithdrawAsync(account.Id, new AccountAmountRequest(10m), CancellationToken.None));
    }

    [Fact]
    public async Task CloseAsync_WithZeroBalanceAccount_ClosesAccount()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(currentUserId);
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accountRepository
            .Setup(repository => repository.UpdateAsync(account, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.CloseAsync(account.Id, CancellationToken.None);

        Assert.True(account.IsClosed);
        Assert.NotNull(account.ClosedAt);
        accountRepository.Verify(
            repository => repository.UpdateAsync(account, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CloseAsync_WithPositiveBalanceAccount_ThrowsValidationException()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(currentUserId, balance: 100m);
        accountRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CloseAsync(account.Id, CancellationToken.None));

        Assert.False(account.IsClosed);
        Assert.Null(account.ClosedAt);
        accountRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithForeignAccountForEmployee_ReturnsAccount()
    {
        ConfigureCurrentUser(currentUserId, isEmployee: true);
        var account = CreateAccount(Guid.NewGuid(), balance: 50m);
        accountRepository
            .Setup(repository => repository.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        var response = await service.GetByIdAsync(account.Id, CancellationToken.None);

        Assert.Equal(account.Id, response.Id);
        Assert.Equal(account.UserId, response.UserId);
        Assert.Equal(account.Balance, response.Balance);
    }

    [Fact]
    public async Task GetOperationsAsync_WithValidRequest_ReturnsOperations()
    {
        ConfigureCurrentUser(currentUserId);
        var account = CreateAccount(currentUserId);
        var operations = new[]
        {
            CreateOperation(account.Id, AccountOperationType.Deposit, 10m),
            CreateOperation(account.Id, AccountOperationType.Withdraw, 5m)
        };

        accountRepository
            .Setup(repository => repository.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accountRepository
            .Setup(repository => repository.GetOperationsAsync(account.Id, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operations);

        var service = CreateService();

        var response = await service.GetOperationsAsync(account.Id, 1, 20, CancellationToken.None);

        Assert.Equal(2, response.Count);
        Assert.Equal(AccountOperationType.Deposit, response[0].Type);
        Assert.Equal(AccountOperationType.Withdraw, response[1].Type);
    }

    private AccountService CreateService()
    {
        return new AccountService(
            accountRepository.Object,
            currentUserService.Object,
            NullLogger<AccountService>.Instance);
    }

    private void ConfigureCurrentUser(Guid userId, bool isEmployee = false)
    {
        currentUserService.Setup(service => service.UserId).Returns(userId);
        currentUserService.Setup(service => service.Role).Returns(isEmployee ? "Employee" : "Client");
        currentUserService.Setup(service => service.IsAuthenticated).Returns(true);
        currentUserService.Setup(service => service.IsEmployee).Returns(isEmployee);
    }

    private void ConfigureAnonymousUser()
    {
        currentUserService.Setup(service => service.UserId).Returns((Guid?)null);
        currentUserService.Setup(service => service.Role).Returns((string?)null);
        currentUserService.Setup(service => service.IsAuthenticated).Returns(false);
        currentUserService.Setup(service => service.IsEmployee).Returns(false);
    }

    private static Account CreateAccount(
        Guid userId,
        decimal balance = 0m,
        bool isClosed = false)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Main account",
            Balance = balance,
            IsClosed = isClosed,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ClosedAt = isClosed ? DateTimeOffset.UtcNow.AddHours(-1) : null
        };
    }

    private static AccountOperation CreateOperation(
        Guid accountId,
        AccountOperationType type,
        decimal amount)
    {
        return new AccountOperation
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Type = type,
            Amount = amount,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
    }
}
