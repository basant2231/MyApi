namespace MyApi.Models.DTOs;

public class ReadingProgressResponse
{
    public int CurrentPage { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? UserRating { get; set; }
    public string? Notes { get; set; }
    public double PercentComplete { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}

