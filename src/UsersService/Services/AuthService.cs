using Microsoft.AspNetCore.Identity;
using UsersService.DTOs.Auth;
using UsersService.Entities;
using UsersService.Exceptions;
using UsersService.Repositories;

namespace UsersService.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.GetByPhoneAsync(request.Phone)
            ?? throw new UnauthorizedException("Invalid phone or password.");

        if (user.IsBlocked)
        {
            throw new ForbiddenException("User is blocked.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid phone or password.");
        }

        return new LoginResponse(user.Id, user.Phone, user.Role);
    }
}
