using UsersService.Entities;

namespace UsersService.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    UserRole? Role { get; }

    bool IsAuthenticated { get; }
}
