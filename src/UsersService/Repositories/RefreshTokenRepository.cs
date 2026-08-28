using Microsoft.EntityFrameworkCore;
using UsersService.Data;
using UsersService.Entities;

namespace UsersService.Repositories;

public sealed class RefreshTokenRepository(UsersDbContext dbContext) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash);
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        await dbContext.RefreshTokens.AddAsync(refreshToken);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Update(refreshToken);
        await dbContext.SaveChangesAsync();
    }
}
