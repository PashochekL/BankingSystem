using Microsoft.EntityFrameworkCore;
using AccountsService.Entities;

namespace AccountsService.Data;

public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<AccountOperation> AccountOperations => Set<AccountOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");

            entity.HasKey(account => account.Id);

            entity.Property(account => account.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(account => account.Balance)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(account => account.CreatedAt)
                .IsRequired();

            entity.HasIndex(account => account.UserId);
        });

        modelBuilder.Entity<AccountOperation>(entity =>
        {
            entity.ToTable("account_operations");

            entity.HasKey(operation => operation.Id);

            entity.Property(operation => operation.Type)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(operation => operation.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(operation => operation.CreatedAt)
                .IsRequired();

            entity.HasOne(operation => operation.Account)
                .WithMany(account => account.Operations)
                .HasForeignKey(operation => operation.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(operation => operation.AccountId);
        });
    }
}
