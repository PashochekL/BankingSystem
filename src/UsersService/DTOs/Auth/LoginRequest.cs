namespace UsersService.DTOs.Auth;

public sealed record LoginRequest(
    string Phone,
    string Password);
