using System.Globalization;
using System.Security.Claims;
using UsersService.Entities;

namespace UsersService.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(userId, CultureInfo.InvariantCulture, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var role = httpContextAccessor.HttpContext?.User.FindFirstValue("role");

            return Enum.TryParse<UserRole>(role, out var userRole) ? userRole : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
