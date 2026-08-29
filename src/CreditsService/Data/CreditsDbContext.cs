using Microsoft.EntityFrameworkCore;

namespace CreditsService.Data;

public sealed class CreditsDbContext(DbContextOptions<CreditsDbContext> options) : DbContext(options)
{
}
