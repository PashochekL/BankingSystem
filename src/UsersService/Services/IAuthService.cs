using UsersService.DTOs.Auth;

namespace UsersService.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
