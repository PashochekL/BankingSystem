namespace AccountsService.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }

    bool IsEmployee { get; }
}
