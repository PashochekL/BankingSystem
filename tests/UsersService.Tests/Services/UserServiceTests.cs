using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UsersService.DTOs.Users;
using UsersService.Entities;
using UsersService.Exceptions;
using UsersService.Repositories;
using UsersService.Services;

namespace UsersService.Tests.Services;

public sealed class UserServiceTests
{
    private const string Password = "StrongPassword123!";

    private readonly Mock<IUserRepository> userRepository = new();
    private readonly PasswordHasher<User> passwordHasher = new();

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesUser()
    {
        var request = new CreateUserRequest(" Ivan ", " Petrov ", " +79990001122 ", Password, UserRole.Client);
        User? addedUser = null;

        userRepository
            .Setup(repository => repository.GetByPhoneAsync("+79990001122", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        userRepository
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => addedUser = user)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Ivan", response.FirstName);
        Assert.Equal("Petrov", response.LastName);
        Assert.Equal("+79990001122", response.Phone);
        Assert.Equal(UserRole.Client, response.Role);
        Assert.False(response.IsBlocked);
        Assert.NotNull(addedUser);
        Assert.Equal(response.Id, addedUser.Id);
        Assert.False(string.IsNullOrWhiteSpace(addedUser.PasswordHash));
        Assert.NotEqual(Password, addedUser.PasswordHash);
    }

    [Fact]
    public async Task CreateAsync_WhenPhoneAlreadyExists_ThrowsConflictException()
    {
        var existingUser = CreateUser();
        var request = new CreateUserRequest("Ivan", "Petrov", existingUser.Phone, Password, UserRole.Client);

        userRepository
            .Setup(repository => repository.GetByPhoneAsync(existingUser.Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(request, CancellationToken.None));

        userRepository.Verify(
            repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRole_ThrowsValidationException()
    {
        var request = new CreateUserRequest("Ivan", "Petrov", "+79990001122", Password, (UserRole)999);

        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(request, CancellationToken.None));

        userRepository.Verify(
            repository => repository.GetByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
    {
        var user = CreateUser();
        userRepository
            .Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();

        var response = await service.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.FirstName, response.FirstName);
        Assert.Equal(user.LastName, response.LastName);
        Assert.Equal(user.Phone, response.Phone);
        Assert.Equal(user.Role, response.Role);
        Assert.Equal(user.IsBlocked, response.IsBlocked);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        userRepository
            .Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(userId, CancellationToken.None));
    }

    [Fact]
    public async Task BlockAsync_WithExistingUser_BlocksUser()
    {
        var user = CreateUser();
        userRepository
            .Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository
            .Setup(repository => repository.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.BlockAsync(user.Id, CancellationToken.None);

        Assert.True(user.IsBlocked);
        userRepository.Verify(
            repository => repository.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BlockAsync_WhenUserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        userRepository
            .Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.BlockAsync(userId, CancellationToken.None));

        userRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private UserService CreateService()
    {
        return new UserService(
            userRepository.Object,
            passwordHasher,
            NullLogger<UserService>.Instance);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ivan",
            LastName = "Petrov",
            Phone = "+79990001122",
            PasswordHash = "password-hash",
            Role = UserRole.Client,
            IsBlocked = false,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }
}
