using Microsoft.EntityFrameworkCore;
using CreditsService.Entities;

namespace CreditsService.Data;

public sealed class CreditsDbContext(DbContextOptions<CreditsDbContext> options) : DbContext(options)
{
    public DbSet<CreditTariff> CreditTariffs => Set<CreditTariff>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditTariff>(entity =>
        {
            entity.ToTable("credit_tariffs");

            entity.HasKey(tariff => tariff.Id);

            entity.Property(tariff => tariff.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(tariff => tariff.InterestRate)
                .HasPrecision(5, 2)
                .IsRequired();

            entity.Property(tariff => tariff.CreatedAt)
                .IsRequired();
        });
    }
}
