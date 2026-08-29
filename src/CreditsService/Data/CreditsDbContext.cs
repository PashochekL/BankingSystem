using Microsoft.EntityFrameworkCore;
using CreditsService.Entities;

namespace CreditsService.Data;

public sealed class CreditsDbContext(DbContextOptions<CreditsDbContext> options) : DbContext(options)
{
    public DbSet<CreditTariff> CreditTariffs => Set<CreditTariff>();

    public DbSet<Credit> Credits => Set<Credit>();

    public DbSet<CreditOperation> CreditOperations => Set<CreditOperation>();

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

        modelBuilder.Entity<Credit>(entity =>
        {
            entity.ToTable("credits");

            entity.HasKey(credit => credit.Id);

            entity.Property(credit => credit.InitialAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(credit => credit.RemainingAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(credit => credit.InterestRate)
                .HasPrecision(5, 2)
                .IsRequired();

            entity.Property(credit => credit.CreatedAt)
                .IsRequired();

            entity.Property(credit => credit.LastInterestAccrualAt)
                .IsRequired();

            entity.Property(credit => credit.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.HasOne(credit => credit.Tariff)
                .WithMany(tariff => tariff.Credits)
                .HasForeignKey(credit => credit.TariffId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(credit => credit.UserId);

            entity.HasIndex(credit => credit.TariffId);
        });

        modelBuilder.Entity<CreditOperation>(entity =>
        {
            entity.ToTable("credit_operations");

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

            entity.HasOne(operation => operation.Credit)
                .WithMany(credit => credit.Operations)
                .HasForeignKey(operation => operation.CreditId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(operation => operation.CreditId);
        });
    }
}
