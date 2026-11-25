using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();
    public DbSet<ReadingProgress> ReadingProgress => Set<ReadingProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(254).IsRequired();
            entity.Property(u => u.Username).HasMaxLength(30).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();

            entity.HasMany(u => u.RefreshTokens)
                  .WithOne(rt => rt.User)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RefreshToken ──
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.Property(rt => rt.Token).HasMaxLength(256).IsRequired();
            entity.Property(rt => rt.CreatedByIp).HasMaxLength(45);
            entity.Property(rt => rt.RevokedByIp).HasMaxLength(45);
        });

        // ── Book ──
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasIndex(b => b.Title);
            entity.HasIndex(b => b.Author);
            entity.HasIndex(b => b.Genre);
            entity.HasIndex(b => b.Isbn).IsUnique()
                  .HasFilter("[Isbn] IS NOT NULL"); // Only enforce uniqueness when ISBN is set

            entity.Property(b => b.Title).HasMaxLength(200).IsRequired();
            entity.Property(b => b.Author).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Description).HasMaxLength(2000);
            entity.Property(b => b.Isbn).HasMaxLength(17);
            entity.Property(b => b.Genre).HasMaxLength(50);
            entity.Property(b => b.Language).HasMaxLength(30);
            entity.Property(b => b.CoverImageUrl).HasMaxLength(500);

            entity.HasOne(b => b.AddedByUser)
                  .WithMany()
                  .HasForeignKey(b => b.AddedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── UserFavorite (many-to-many: User <-> Book) ──
        modelBuilder.Entity<UserFavorite>(entity =>
        {
            // A user can only favorite a book once
            entity.HasIndex(uf => new { uf.UserId, uf.BookId }).IsUnique();

            entity.HasOne(uf => uf.User)
                  .WithMany()
                  .HasForeignKey(uf => uf.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uf => uf.Book)
                  .WithMany(b => b.FavoritedBy)
                  .HasForeignKey(uf => uf.BookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ReadingProgress (one per user per book) ──
        modelBuilder.Entity<ReadingProgress>(entity =>
        {
            entity.HasIndex(rp => new { rp.UserId, rp.BookId }).IsUnique();

            entity.HasOne(rp => rp.User)
                  .WithMany()
                  .HasForeignKey(rp => rp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Book)
                  .WithMany(b => b.ReadingProgress)
                  .HasForeignKey(rp => rp.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(rp => rp.Notes).HasMaxLength(1000);
        });
    }
}
