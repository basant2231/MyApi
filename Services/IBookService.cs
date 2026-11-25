using MyApi.Models.DTOs;

namespace MyApi.Services;

public interface IBookService
{
    // ── CRUD ──
    Task<BookResponse> CreateBookAsync(CreateBookRequest request, int userId);
    Task<BookResponse> GetBookByIdAsync(int bookId, int? userId);
    Task<PaginatedResponse<BookResponse>> GetBooksAsync(
        int page, int pageSize, string? search, string? genre,
        string? sortBy, bool descending, int? userId);
    Task<BookResponse> UpdateBookAsync(int bookId, UpdateBookRequest request, int userId);
    Task DeleteBookAsync(int bookId, int userId);

    // ── Favorites ──
    Task<BookResponse> ToggleFavoriteAsync(int bookId, int userId);
    Task<PaginatedResponse<BookResponse>> GetFavoritesAsync(int userId, int page, int pageSize);

    // ── Reading Progress ──
    Task<ReadingProgressResponse> UpdateReadingProgressAsync(
        int bookId, UpdateReadingProgressRequest request, int userId);
    Task<PaginatedResponse<BookResponse>> GetMyBooksAsync(
        int userId, int? statusFilter, int page, int pageSize);

    // ── Genres ──
    Task<List<string>> GetGenresAsync();
}
