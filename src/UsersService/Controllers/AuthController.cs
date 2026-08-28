using Microsoft.AspNetCore.Mvc;
using UsersService.DTOs.Auth;
using UsersService.Services;

namespace UsersService.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var response = await authService.LoginAsync(request);

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshTokenRequest request)
    {
        var response = await authService.RefreshAsync(request);

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request)
    {
        await authService.LogoutAsync(request);

        return NoContent();
    }
}
