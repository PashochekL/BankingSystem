using UsersService.Entities;

namespace UsersService.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
}
