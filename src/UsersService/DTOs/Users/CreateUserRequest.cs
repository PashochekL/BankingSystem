using UsersService.Entities;

namespace UsersService.DTOs.Users;

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Phone,
    string PasswordHash,
    UserRole Role);
