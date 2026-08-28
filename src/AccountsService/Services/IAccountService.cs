using AccountsService.DTOs.Accounts;

namespace AccountsService.Services;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(CreateAccountRequest request);

    Task<IReadOnlyList<AccountResponse>> GetAllAsync();

    Task<AccountResponse> GetByIdAsync(Guid id);

    Task CloseAsync(Guid id);
}
