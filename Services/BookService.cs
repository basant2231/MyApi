using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Models.DTOs;

namespace MyApi.Services;

public class BookService : IBookService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookService> _logger;

    public BookService(AppDbContext db, ILogger<BookService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ────────────── CRUD ──────────────

    public async Task<BookResponse> CreateBookAsync(CreateBookRequest request, int userId)
    {
        // Check duplicate ISBN
        if (!string.IsNullOrEmpty(request.Isbn))
        {
            if (await _db.Books.AnyAsync(b => b.Isbn == request.Isbn))
                throw new InvalidOperationException("A book with this ISBN already exists.");
        }

        var book = new Book
        {
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Description = request.Description?.Trim(),
            Isbn = request.Isbn?.Trim(),
            CoverImageUrl = request.CoverImageUrl?.Trim(),
            PageCount = request.PageCount,
            Genre = request.Genre?.Trim(),
            Language = request.Language?.Trim(),
            PublishedDate = request.PublishedDate,
            AddedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Book '{Title}' created by user {UserId}", book.Title, userId);

        return await GetBookByIdAsync(book.Id, userId);
    }

    public async Task<BookResponse> GetBookByIdAsync(int bookId, int? userId)
    {
        var book = await _db.Books
            .Include(b => b.AddedByUser)
            .Include(b => b.FavoritedBy)
            .Include(b => b.ReadingProgress)
            .FirstOrDefaultAsync(b => b.Id == bookId)
            ?? throw new KeyNotFoundException("Book not found.");

        return MapToResponse(book, userId);
    }

    public async Task<PaginatedResponse<BookResponse>> GetBooksAsync(
        int page, int pageSize, string? search, string? genre,
        string? sortBy, bool descending, int? userId)
    {
        var query = _db.Books
            .Include(b => b.AddedByUser)
            .Include(b => b.FavoritedBy)
            .Include(b => b.ReadingProgress)
            .AsQueryable();

        // ── Search by title or author ──
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(term) ||
                b.Author.ToLower().Contains(term));
        }

        // ── Filter by genre ──
        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(b => b.Genre != null && b.Genre.ToLower() == genre.Trim().ToLower());
        }

        // ── Sort ──
        query = (sortBy?.ToLower()) switch
        {
            "title" => descending ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
            "author" => descending ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
            "rating" => descending ? query.OrderByDescending(b => b.Rating) : query.OrderBy(b => b.Rating),
            "date" => descending ? query.OrderByDescending(b => b.PublishedDate) : query.OrderBy(b => b.PublishedDate),
            "favorites" => descending
                ? query.OrderByDescending(b => b.FavoritedBy.Count)
                : query.OrderBy(b => b.FavoritedBy.Count),
            _ => query.OrderByDescending(b => b.CreatedAt) // Default: newest first
        };

        return await PaginateAsync(query, page, pageSize, userId);
    }

    public async Task<BookResponse> UpdateBookAsync(int bookId, UpdateBookRequest request, int userId)
    {
        var book = await _db.Books.FindAsync(bookId)
            ?? throw new KeyNotFoundException("Book not found.");

        // Only the user who added the book can update it
        if (book.AddedByUserId != userId)
            throw new UnauthorizedAccessException("You can only edit books you added.");

        // Check duplicate ISBN (if changing)
        if (!string.IsNullOrEmpty(request.Isbn) && request.Isbn != book.Isbn)
        {
            if (await _db.Books.AnyAsync(b => b.Isbn == request.Isbn && b.Id != bookId))
                throw new InvalidOperationException("A book with this ISBN already exists.");
        }

        // Update only provided fields (partial update)
        if (request.Title is not null) book.Title = request.Title.Trim();
        if (request.Author is not null) book.Author = request.Author.Trim();
        if (request.Description is not null) book.Description = request.Description.Trim();
        if (request.Isbn is not null) book.Isbn = request.Isbn.Trim();
        if (request.CoverImageUrl is not null) book.CoverImageUrl = request.CoverImageUrl.Trim();
        if (request.PageCount.HasValue) book.PageCount = request.PageCount;
        if (request.Genre is not null) book.Genre = request.Genre.Trim();
        if (request.Language is not null) book.Language = request.Language.Trim();
        if (request.PublishedDate.HasValue) book.PublishedDate = request.PublishedDate;

        book.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Book {BookId} updated by user {UserId}", bookId, userId);

        return await GetBookByIdAsync(bookId, userId);
    }

    public async Task DeleteBookAsync(int bookId, int userId)
    {
        var book = await _db.Books.FindAsync(bookId)
            ?? throw new KeyNotFoundException("Book not found.");

        if (book.AddedByUserId != userId)
            throw new UnauthorizedAccessException("You can only delete books you added.");

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Book {BookId} deleted by user {UserId}", bookId, userId);
    }

    // ────────────── FAVORITES ──────────────

    public async Task<BookResponse> ToggleFavoriteAsync(int bookId, int userId)
    {
        var book = await _db.Books.FindAsync(bookId)
            ?? throw new KeyNotFoundException("Book not found.");

        var existing = await _db.UserFavorites
            .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.BookId == bookId);

        if (existing is not null)
        {
            _db.UserFavorites.Remove(existing);
            _logger.LogInformation("User {UserId} unfavorited book {BookId}", userId, bookId);
        }
        else
        {
            _db.UserFavorites.Add(new UserFavorite
            {
                UserId = userId,
                BookId = bookId,
                CreatedAt = DateTime.UtcNow
            });
            _logger.LogInformation("User {UserId} favorited book {BookId}", userId, bookId);
        }

        await _db.SaveChangesAsync();
        return await GetBookByIdAsync(bookId, userId);
    }

    public async Task<PaginatedResponse<BookResponse>> GetFavoritesAsync(int userId, int page, int pageSize)
    {
        var query = _db.Books
            .Include(b => b.AddedByUser)
            .Include(b => b.FavoritedBy)
            .Include(b => b.ReadingProgress)
            .Where(b => b.FavoritedBy.Any(uf => uf.UserId == userId))
            .OrderByDescending(b => b.FavoritedBy
                .Where(uf => uf.UserId == userId)
                .Select(uf => uf.CreatedAt)
                .FirstOrDefault());

        return await PaginateAsync(query, page, pageSize, userId);
    }

    // ────────────── READING PROGRESS ──────────────

    public async Task<ReadingProgressResponse> UpdateReadingProgressAsync(
        int bookId, UpdateReadingProgressRequest request, int userId)
    {
        var book = await _db.Books.FindAsync(bookId)
            ?? throw new KeyNotFoundException("Book not found.");

        var progress = await _db.ReadingProgress
            .FirstOrDefaultAsync(rp => rp.UserId == userId && rp.BookId == bookId);

        if (progress is null)
        {
            progress = new ReadingProgress
            {
                UserId = userId,
                BookId = bookId,
                StartedAt = DateTime.UtcNow
            };
            _db.ReadingProgress.Add(progress);
        }

        if (request.CurrentPage.HasValue) progress.CurrentPage = request.CurrentPage.Value;
        if (request.Status.HasValue)
        {
            progress.Status = (ReadingStatus)request.Status.Value;
            if (progress.Status == ReadingStatus.Finished)
                progress.FinishedAt = DateTime.UtcNow;
        }
        if (request.UserRating.HasValue) progress.UserRating = request.UserRating;
        if (request.Notes is not null) progress.Notes = request.Notes;

        progress.UpdatedAt = DateTime.UtcNow;

        // Recalculate book's average rating from all user ratings
        await RecalculateBookRating(bookId);

        await _db.SaveChangesAsync();

        return MapProgressToResponse(progress, book.PageCount);
    }

    public async Task<PaginatedResponse<BookResponse>> GetMyBooksAsync(
        int userId, int? statusFilter, int page, int pageSize)
    {
        var query = _db.Books
            .Include(b => b.AddedByUser)
            .Include(b => b.FavoritedBy)
            .Include(b => b.ReadingProgress)
            .Where(b => b.ReadingProgress.Any(rp => rp.UserId == userId))
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            var status = (ReadingStatus)statusFilter.Value;
            query = query.Where(b => b.ReadingProgress
                .Any(rp => rp.UserId == userId && rp.Status == status));
        }

        query = query.OrderByDescending(b => b.ReadingProgress
            .Where(rp => rp.UserId == userId)
            .Select(rp => rp.UpdatedAt)
            .FirstOrDefault());

        return await PaginateAsync(query, page, pageSize, userId);
    }

    // ────────────── GENRES ──────────────

    public async Task<List<string>> GetGenresAsync()
    {
        return await _db.Books
            .Where(b => b.Genre != null)
            .Select(b => b.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }

    // ────────────── HELPERS ──────────────

    private static BookResponse MapToResponse(Book book, int? userId)
    {
        var response = new BookResponse
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Description = book.Description,
            Isbn = book.Isbn,
            CoverImageUrl = book.CoverImageUrl,
            PageCount = book.PageCount,
            Genre = book.Genre,
            Language = book.Language,
            PublishedDate = book.PublishedDate,
            Rating = book.Rating,
            CreatedAt = book.CreatedAt,
            AddedByUsername = book.AddedByUser?.Username ?? "unknown",
            FavoriteCount = book.FavoritedBy.Count
        };

        if (userId.HasValue)
        {
            response.IsFavorited = book.FavoritedBy.Any(uf => uf.UserId == userId.Value);

            var progress = book.ReadingProgress
                .FirstOrDefault(rp => rp.UserId == userId.Value);
            if (progress is not null)
                response.MyReadingProgress = MapProgressToResponse(progress, book.PageCount);
        }

        return response;
    }

    private static ReadingProgressResponse MapProgressToResponse(ReadingProgress progress, int? totalPages)
    {
        return new ReadingProgressResponse
        {
            CurrentPage = progress.CurrentPage,
            Status = progress.Status.ToString(),
            UserRating = progress.UserRating,
            Notes = progress.Notes,
            PercentComplete = totalPages > 0
                ? Math.Round((double)progress.CurrentPage / totalPages.Value * 100, 1)
                : 0,
            StartedAt = progress.StartedAt,
            FinishedAt = progress.FinishedAt
        };
    }

    private async Task RecalculateBookRating(int bookId)
    {
        var ratings = await _db.ReadingProgress
            .Where(rp => rp.BookId == bookId && rp.UserRating.HasValue)
            .Select(rp => rp.UserRating!.Value)
            .ToListAsync();

        var book = await _db.Books.FindAsync(bookId);
        if (book is not null)
        {
            book.Rating = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : null;
        }
    }

    private static async Task<PaginatedResponse<BookResponse>> PaginateAsync(
        IQueryable<Book> query, int page, int pageSize, int? userId)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<BookResponse>
        {
            Items = items.Select(b => MapToResponse(b, userId)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };
    }
}
