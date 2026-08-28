using AccountsService.DTOs.Accounts;

namespace AccountsService.Services;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(CreateAccountRequest request);

    Task<IReadOnlyList<AccountResponse>> GetAllAsync();

    Task<AccountResponse> GetByIdAsync(Guid id);

    Task CloseAsync(Guid id);

    Task<AccountResponse> DepositAsync(Guid id, AccountAmountRequest request);

    Task<AccountResponse> WithdrawAsync(Guid id, AccountAmountRequest request);

    Task<IReadOnlyList<AccountOperationResponse>> GetOperationsAsync(Guid id, int page, int pageSize);
}
