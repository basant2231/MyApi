namespace MyApi.Models.DTOs;

/// <summary>
/// Book data returned to the client.
/// </summary>
public class BookResponse
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
    public DateTime CreatedAt { get; set; }
    public string AddedByUsername { get; set; } = string.Empty;
    public bool IsFavorited { get; set; }
    public int FavoriteCount { get; set; }
    public ReadingProgressResponse? MyReadingProgress { get; set; }
}

