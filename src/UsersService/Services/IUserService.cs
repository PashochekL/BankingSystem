using UsersService.DTOs.Users;

namespace UsersService.Services;

public interface IUserService
{
    Task<UserResponse?> CreateAsync(CreateUserRequest request);

    Task<UserResponse?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<UserResponse>> GetAllAsync();

    Task<bool> BlockAsync(Guid id);
}
