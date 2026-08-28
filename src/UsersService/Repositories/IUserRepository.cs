using UsersService.Entities;

namespace UsersService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByPhoneAsync(string phone);

    Task<IReadOnlyList<User>> GetAllAsync();

    Task AddAsync(User user);

    Task UpdateAsync(User user);
}
