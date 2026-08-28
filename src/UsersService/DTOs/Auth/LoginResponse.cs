using UsersService.Entities;

namespace UsersService.DTOs.Auth;

public sealed record LoginResponse(
    Guid UserId,
    string Phone,
    UserRole Role,
    string AccessToken);
