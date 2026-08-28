using Microsoft.EntityFrameworkCore;

namespace UsersService.Data;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
}
