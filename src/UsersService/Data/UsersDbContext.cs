using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Data;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.Phone)
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(user => user.Phone)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasKey(refreshToken => refreshToken.Id);

            entity.Property(refreshToken => refreshToken.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasIndex(refreshToken => refreshToken.TokenHash)
                .IsUnique();

            entity.Property(refreshToken => refreshToken.ExpiresAt)
                .IsRequired();

            entity.Property(refreshToken => refreshToken.CreatedAt)
                .IsRequired();

            entity.HasOne(refreshToken => refreshToken.User)
                .WithMany()
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
