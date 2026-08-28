using Microsoft.AspNetCore.Identity;
using UsersService.DTOs.Users;
using UsersService.Entities;
using UsersService.Exceptions;
using UsersService.Repositories;

namespace UsersService.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        var existingUser = await userRepository.GetByPhoneAsync(request.Phone);
        if (existingUser is not null)
        {
            throw new ConflictException("User with this phone already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = request.Role,
            IsBlocked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await userRepository.AddAsync(user);

        return MapToResponse(user);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User was not found.");

        return MapToResponse(user);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync()
    {
        var users = await userRepository.GetAllAsync();

        return users
            .Select(MapToResponse)
            .ToList();
    }

    public async Task BlockAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User was not found.");

        user.IsBlocked = true;
        await userRepository.UpdateAsync(user);
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
