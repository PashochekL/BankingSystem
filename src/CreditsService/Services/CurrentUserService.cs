using System.Globalization;
using System.Security.Claims;

namespace CreditsService.Services;

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

    public string? Role => httpContextAccessor.HttpContext?.User.FindFirstValue("role");

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsEmployee => string.Equals(Role, "Employee", StringComparison.Ordinal);
}
