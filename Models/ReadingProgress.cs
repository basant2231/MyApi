namespace MyApi.Models;

/// <summary>
/// Tracks a user's reading progress on a book.
/// </summary>
public class ReadingProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    /// <summary>Current page the user is on.</summary>
    public int CurrentPage { get; set; }

    /// <summary>Reading status.</summary>
    public ReadingStatus Status { get; set; } = ReadingStatus.WantToRead;

    /// <summary>User's personal rating (1-5).</summary>
    public int? UserRating { get; set; }

    /// <summary>User's personal notes/review.</summary>
    public string? Notes { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum ReadingStatus
{
    WantToRead = 0,
    Reading = 1,
    Finished = 2,
    Dropped = 3
}
