using UsersService.DTOs.Auth;

namespace UsersService.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);

    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request);

    Task LogoutAsync(LogoutRequest request);
}
