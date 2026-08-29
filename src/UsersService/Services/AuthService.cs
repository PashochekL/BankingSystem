using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using UsersService.DTOs.Auth;
using UsersService.Entities;
using UsersService.Exceptions;
using UsersService.Options;
using UsersService.Repositories;

namespace UsersService.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher<User> passwordHasher,
    IJwtService jwtService,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        UserRequestValidation.ValidatePhone(request.Phone);
        UserRequestValidation.ValidateLoginPassword(request.Password);

        var user = await userRepository.GetByPhoneAsync(request.Phone.Trim(), cancellationToken)
            ?? throw new UnauthorizedException("Invalid phone or password.");

        if (user.IsBlocked)
        {
            logger.LogWarning("Blocked user {UserId} attempted to log in", user.Id);
            throw new ForbiddenException("User is blocked.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login failed for user {UserId}", user.Id);
            throw new UnauthorizedException("Invalid phone or password.");
        }

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user, cancellationToken);

        logger.LogInformation("User {UserId} logged in", user.Id);

        return new LoginResponse(user.Id, user.Phone, user.Role, accessToken, refreshToken);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        UserRequestValidation.ValidateRefreshToken(request.RefreshToken);

        var refreshTokenHash = HashRefreshToken(request.RefreshToken);
        var savedRefreshToken = await refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (savedRefreshToken.RevokedAt is not null || savedRefreshToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (savedRefreshToken.User.IsBlocked)
        {
            throw new ForbiddenException("User is blocked.");
        }

        savedRefreshToken.RevokedAt = DateTimeOffset.UtcNow;
        await refreshTokenRepository.UpdateAsync(savedRefreshToken, cancellationToken);

        var accessToken = jwtService.GenerateAccessToken(savedRefreshToken.User);
        var newRefreshToken = await CreateRefreshTokenAsync(savedRefreshToken.User, cancellationToken);

        logger.LogInformation("Refresh session rotated for user {UserId}", savedRefreshToken.UserId);

        return new LoginResponse(
            savedRefreshToken.User.Id,
            savedRefreshToken.User.Phone,
            savedRefreshToken.User.Role,
            accessToken,
            newRefreshToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        UserRequestValidation.ValidateRefreshToken(request.RefreshToken);

        var refreshTokenHash = HashRefreshToken(request.RefreshToken);
        var savedRefreshToken = await refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (savedRefreshToken.RevokedAt is null)
        {
            savedRefreshToken.RevokedAt = DateTimeOffset.UtcNow;
            await refreshTokenRepository.UpdateAsync(savedRefreshToken, cancellationToken);

            logger.LogInformation("Refresh session revoked for user {UserId}", savedRefreshToken.UserId);
        }
    }

    private async Task<string> CreateRefreshTokenAsync(User user, CancellationToken cancellationToken)
    {
        var refreshToken = GenerateRefreshToken();
        var now = DateTimeOffset.UtcNow;

        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashRefreshToken(refreshToken),
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenExpirationDays),
            CreatedAt = now
        }, cancellationToken);

        return refreshToken;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));

        return Convert.ToHexString(hash);
    }
}
