using UsersService.DTOs.Users;

namespace UsersService.Services;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task BlockAsync(Guid id, CancellationToken cancellationToken);
}
