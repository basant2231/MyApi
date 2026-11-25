using System.ComponentModel.DataAnnotations;

namespace MyApi.Models.DTOs;

public class UpdateReadingProgressRequest
{
    [Range(0, 50000, ErrorMessage = "Current page must be between 0 and 50000.")]
    public int? CurrentPage { get; set; }

    /// <summary>0=WantToRead, 1=Reading, 2=Finished, 3=Dropped</summary>
    [Range(0, 3, ErrorMessage = "Status must be 0 (WantToRead), 1 (Reading), 2 (Finished), or 3 (Dropped).")]
    public int? Status { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int? UserRating { get; set; }

    [MaxLength(1000, ErrorMessage = "Notes must not exceed 1000 characters.")]
    public string? Notes { get; set; }
}
