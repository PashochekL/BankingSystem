using UsersService.Entities;

namespace UsersService.DTOs.Users;

public sealed record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    UserRole Role,
    bool IsBlocked,
    DateTimeOffset CreatedAt);
