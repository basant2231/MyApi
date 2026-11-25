using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data;

/// <summary>
/// The database context — like a "database manager" class.
/// Entity Framework Core uses this to create/query tables.
/// 
/// Think of DbSet<User> as a "table" of users.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Make email unique — no duplicate accounts
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Make username unique too
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}

