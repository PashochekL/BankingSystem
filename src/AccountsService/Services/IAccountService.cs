using AccountsService.DTOs.Accounts;

namespace AccountsService.Services;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<AccountResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task CloseAsync(Guid id, CancellationToken cancellationToken);

    Task<AccountResponse> DepositAsync(Guid id, AccountAmountRequest request, CancellationToken cancellationToken);

    Task<AccountResponse> WithdrawAsync(Guid id, AccountAmountRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountOperationResponse>> GetOperationsAsync(
        Guid id,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
