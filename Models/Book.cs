namespace MyApi.Models;

/// <summary>
/// Book entity — the core of your book app.
/// Each book belongs to the user who added it.
/// </summary>
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Isbn { get; set; }
    public string? CoverImageUrl { get; set; }
    public int? PageCount { get; set; }
    public string? Genre { get; set; }
    public string? Language { get; set; }
    public DateTime? PublishedDate { get; set; }
    public double? Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ── Who added this book ──
    public int AddedByUserId { get; set; }
    public User AddedByUser { get; set; } = null!;

    // ── Navigation ──
    public List<UserFavorite> FavoritedBy { get; set; } = [];
    public List<ReadingProgress> ReadingProgress { get; set; } = [];
}

