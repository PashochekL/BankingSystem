using Microsoft.AspNetCore.Identity;
using UsersService.DTOs.Users;
using UsersService.Entities;
using UsersService.Exceptions;
using UsersService.Repositories;

namespace UsersService.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    ICurrentUserService currentUserService,
    ILogger<UserService> logger) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        UserRequestValidation.ValidateName(request.FirstName, "First name");
        UserRequestValidation.ValidateName(request.LastName, "Last name");
        UserRequestValidation.ValidatePhone(request.Phone);
        UserRequestValidation.ValidatePassword(request.Password);
        UserRequestValidation.ValidateRole(request.Role);

        var phone = request.Phone.Trim();
        var existingUser = await userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (existingUser is not null)
        {
            throw new ConflictException("User with this phone already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = phone,
            Role = request.Role,
            IsBlocked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await userRepository.AddAsync(user, cancellationToken);

        logger.LogInformation("User {UserId} created with role {Role}", user.Id, user.Role);

        return MapToResponse(user);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureCanAccess(id);

        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        return MapToResponse(user);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        return users
            .Select(MapToResponse)
            .ToList();
    }

    public async Task BlockAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        if (currentUserService.UserId == id)
        {
            throw new ForbiddenException("User cannot block himself.");
        }

        user.IsBlocked = true;
        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("User {UserId} blocked", user.Id);
    }

    private void EnsureCanAccess(Guid userId)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not { } currentUserId)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        if (currentUserService.Role != UserRole.Employee && currentUserId != userId)
        {
            throw new ForbiddenException("User access is forbidden.");
        }
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
