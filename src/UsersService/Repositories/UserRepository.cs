using Microsoft.EntityFrameworkCore;
using UsersService.Data;
using UsersService.Entities;

namespace UsersService.Repositories;

public sealed class UserRepository(UsersDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Phone == phone);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();
    }
}
