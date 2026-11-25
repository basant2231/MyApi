using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data;

/// <summary>
/// The database context — manages all database tables and relationships.
/// Now includes RefreshTokens and proper indexing.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User Configuration ──
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();

            entity.Property(u => u.Email).HasMaxLength(254).IsRequired();
            entity.Property(u => u.Username).HasMaxLength(30).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();

            // One user has many refresh tokens
            entity.HasMany(u => u.RefreshTokens)
                  .WithOne(rt => rt.User)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RefreshToken Configuration ──
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.Property(rt => rt.Token).HasMaxLength(256).IsRequired();
            entity.Property(rt => rt.CreatedByIp).HasMaxLength(45); // IPv6 max length
            entity.Property(rt => rt.RevokedByIp).HasMaxLength(45);
        });
    }
}
