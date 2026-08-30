using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UsersService.DTOs.Auth;
using UsersService.Entities;
using UsersService.Exceptions;
using UsersService.Options;
using UsersService.Repositories;
using UsersService.Services;

namespace UsersService.Tests.Services;

public sealed class AuthServiceTests
{
    private const string Password = "StrongPassword123!";
    private const string AccessToken = "access-token";
    private const int RefreshTokenExpirationDays = 7;

    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IRefreshTokenRepository> refreshTokenRepository = new();
    private readonly Mock<IJwtService> jwtService = new();
    private readonly PasswordHasher<User> passwordHasher = new();

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndCreatesRefreshToken()
    {
        var user = CreateUser();
        user.PasswordHash = passwordHasher.HashPassword(user, Password);
        RefreshToken? addedRefreshToken = null;

        userRepository
            .Setup(repository => repository.GetByPhoneAsync(user.Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        jwtService
            .Setup(service => service.GenerateAccessToken(user))
            .Returns(AccessToken);
        refreshTokenRepository
            .Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((refreshToken, _) => addedRefreshToken = refreshToken)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.LoginAsync(new LoginRequest(user.Phone, Password), CancellationToken.None);

        Assert.Equal(user.Id, response.UserId);
        Assert.Equal(user.Phone, response.Phone);
        Assert.Equal(user.Role, response.Role);
        Assert.Equal(AccessToken, response.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.NotNull(addedRefreshToken);
        Assert.Equal(user.Id, addedRefreshToken.UserId);
        Assert.False(string.IsNullOrWhiteSpace(addedRefreshToken.TokenHash));
        Assert.True(addedRefreshToken.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedException()
    {
        var user = CreateUser();
        user.PasswordHash = passwordHasher.HashPassword(user, Password);
        userRepository
            .Setup(repository => repository.GetByPhoneAsync(user.Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new LoginRequest(user.Phone, "WrongPassword123!"), CancellationToken.None));

        jwtService.Verify(service => service.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        refreshTokenRepository.Verify(
            repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserBlocked_ThrowsForbiddenException()
    {
        var user = CreateUser(isBlocked: true);
        user.PasswordHash = passwordHasher.HashPassword(user, Password);
        userRepository
            .Setup(repository => repository.GetByPhoneAsync(user.Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.LoginAsync(new LoginRequest(user.Phone, Password), CancellationToken.None));

        jwtService.Verify(service => service.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        userRepository
            .Setup(repository => repository.GetByPhoneAsync("+79990001122", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new LoginRequest("+79990001122", Password), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WithValidRefreshToken_ReturnsNewTokensAndRevokesOldToken()
    {
        var user = CreateUser();
        var existingRefreshTokenValue = "current-refresh-token";
        var savedRefreshToken = CreateRefreshToken(user, existingRefreshTokenValue);
        RefreshToken? addedRefreshToken = null;

        refreshTokenRepository
            .Setup(repository => repository.GetByTokenHashAsync(HashRefreshToken(existingRefreshTokenValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedRefreshToken);
        refreshTokenRepository
            .Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((refreshToken, _) => addedRefreshToken = refreshToken)
            .Returns(Task.CompletedTask);
        refreshTokenRepository
            .Setup(repository => repository.UpdateAsync(savedRefreshToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        jwtService
            .Setup(service => service.GenerateAccessToken(user))
            .Returns(AccessToken);

        var service = CreateService();

        var response = await service.RefreshAsync(new RefreshTokenRequest(existingRefreshTokenValue), CancellationToken.None);

        Assert.Equal(user.Id, response.UserId);
        Assert.Equal(AccessToken, response.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.NotNull(savedRefreshToken.RevokedAt);
        Assert.NotNull(addedRefreshToken);
        Assert.Equal(user.Id, addedRefreshToken.UserId);
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredRefreshToken_ThrowsUnauthorizedException()
    {
        var user = CreateUser();
        var refreshTokenValue = "expired-refresh-token";
        var savedRefreshToken = CreateRefreshToken(user, refreshTokenValue);
        savedRefreshToken.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        refreshTokenRepository
            .Setup(repository => repository.GetByTokenHashAsync(HashRefreshToken(refreshTokenValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedRefreshToken);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.RefreshAsync(new RefreshTokenRequest(refreshTokenValue), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedRefreshToken_ThrowsUnauthorizedException()
    {
        var user = CreateUser();
        var refreshTokenValue = "revoked-refresh-token";
        var savedRefreshToken = CreateRefreshToken(user, refreshTokenValue);
        savedRefreshToken.RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        refreshTokenRepository
            .Setup(repository => repository.GetByTokenHashAsync(HashRefreshToken(refreshTokenValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedRefreshToken);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.RefreshAsync(new RefreshTokenRequest(refreshTokenValue), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WithInvalidRefreshToken_ThrowsUnauthorizedException()
    {
        var refreshTokenValue = "missing-refresh-token";
        refreshTokenRepository
            .Setup(repository => repository.GetByTokenHashAsync(HashRefreshToken(refreshTokenValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.RefreshAsync(new RefreshTokenRequest(refreshTokenValue), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WhenUserBlocked_ThrowsForbiddenException()
    {
        var user = CreateUser(isBlocked: true);
        var refreshTokenValue = "blocked-refresh-token";
        var savedRefreshToken = CreateRefreshToken(user, refreshTokenValue);

        refreshTokenRepository
            .Setup(repository => repository.GetByTokenHashAsync(HashRefreshToken(refreshTokenValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedRefreshToken);

        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.RefreshAsync(new RefreshTokenRequest(refreshTokenValue), CancellationToken.None));
    }

    [Fact]
    public async Task LogoutAsync_WithValidRefreshToken_RevokesRefreshToken()
    {
        var user = CreateUser();
        var refreshTokenValue = "logout-refresh-token";
        var savedRefreshToken = CreateRefreshToken(user, refreshTokenValue);

        refreshTokenRepository
            .Setup(repository => repository.GetByTokenHashAsync(HashRefreshToken(refreshTokenValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedRefreshToken);
        refreshTokenRepository
            .Setup(repository => repository.UpdateAsync(savedRefreshToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.LogoutAsync(new LogoutRequest(refreshTokenValue), CancellationToken.None);

        Assert.NotNull(savedRefreshToken.RevokedAt);
        refreshTokenRepository.Verify(
            repository => repository.UpdateAsync(savedRefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidRefreshToken_ThrowsUnauthorizedException()
    {
        var refreshTokenValue = "invalid-logout-token";
        refreshTokenRepository
            .Setup(repository => repository.GetByTokenHashAsync(HashRefreshToken(refreshTokenValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LogoutAsync(new LogoutRequest(refreshTokenValue), CancellationToken.None));
    }

    private AuthService CreateService()
    {
        return new AuthService(
            userRepository.Object,
            refreshTokenRepository.Object,
            passwordHasher,
            jwtService.Object,
            Microsoft.Extensions.Options.Options.Create(new JwtOptions
            {
                Secret = "unit-test-secret-unit-test-secret-unit-test-secret",
                Issuer = "users-tests",
                Audience = "users-tests",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = RefreshTokenExpirationDays
            }),
            NullLogger<AuthService>.Instance);
    }

    private static User CreateUser(bool isBlocked = false)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ivan",
            LastName = "Petrov",
            Phone = "+79990001122",
            Role = UserRole.Client,
            IsBlocked = isBlocked,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    private static RefreshToken CreateRefreshToken(User user, string refreshTokenValue)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = HashRefreshToken(refreshTokenValue),
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));

        return Convert.ToHexString(hash);
    }
}
