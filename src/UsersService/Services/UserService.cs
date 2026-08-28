using UsersService.DTOs.Users;
using UsersService.Entities;
using UsersService.Repositories;

namespace UsersService.Services;

public sealed class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<UserResponse?> CreateAsync(CreateUserRequest request)
    {
        var existingUser = await userRepository.GetByPhoneAsync(request.Phone);
        if (existingUser is not null)
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            PasswordHash = request.PasswordHash,
            Role = request.Role,
            IsBlocked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await userRepository.AddAsync(user);

        return MapToResponse(user);
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);

        return user is null ? null : MapToResponse(user);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync()
    {
        var users = await userRepository.GetAllAsync();

        return users
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<bool> BlockAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return false;
        }

        user.IsBlocked = true;
        await userRepository.UpdateAsync(user);

        return true;
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.Role,
            user.IsBlocked,
            user.CreatedAt);
    }
}
