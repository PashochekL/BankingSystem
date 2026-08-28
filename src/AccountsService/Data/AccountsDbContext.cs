using Microsoft.EntityFrameworkCore;

namespace AccountsService.Data;

public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
{
}
