using UsersService.Entities;

namespace UsersService.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

    Task AddAsync(RefreshToken refreshToken);

    Task UpdateAsync(RefreshToken refreshToken);
}
