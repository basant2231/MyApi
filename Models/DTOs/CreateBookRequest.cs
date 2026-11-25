using System.ComponentModel.DataAnnotations;

namespace MyApi.Models.DTOs;

public class CreateBookRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title must not exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author is required.")]
    [MaxLength(100, ErrorMessage = "Author must not exceed 100 characters.")]
    public string Author { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Description must not exceed 2000 characters.")]
    public string? Description { get; set; }

    [MaxLength(13, ErrorMessage = "ISBN must not exceed 13 characters.")]
    public string? Isbn { get; set; }

    [Url(ErrorMessage = "Invalid URL format.")]
    public string? CoverImageUrl { get; set; }

    [Range(1, 50000, ErrorMessage = "Page count must be between 1 and 50000.")]
    public int? PageCount { get; set; }

    [MaxLength(50, ErrorMessage = "Genre must not exceed 50 characters.")]
    public string? Genre { get; set; }

    [MaxLength(30, ErrorMessage = "Language must not exceed 30 characters.")]
    public string? Language { get; set; }

    public DateTime? PublishedDate { get; set; }
}

