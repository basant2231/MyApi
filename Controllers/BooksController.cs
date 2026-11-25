using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models;
using MyApi.Models.DTOs;
using MyApi.Services;

namespace MyApi.Controllers;

/// <summary>
/// BOOKS CONTROLLER — Full CRUD with search, favorites, and reading progress.
/// 
/// PUBLIC (no token needed):
///   GET  /api/books              → Browse all books (search, filter, paginate)
///   GET  /api/books/{id}         → Get book details
///   GET  /api/books/genres       → List all genres
/// 
/// PROTECTED (requires token):
///   POST   /api/books            → Add a new book
///   PUT    /api/books/{id}       → Update a book (only your own)
///   DELETE /api/books/{id}       → Delete a book (only your own)
///   POST   /api/books/{id}/favorite     → Toggle favorite on/off
///   GET    /api/books/my/favorites      → Get my favorited books
///   PUT    /api/books/{id}/progress     → Update reading progress
///   GET    /api/books/my/library        → Get my reading library
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IValidator<CreateBookRequest> _createValidator;
    private readonly IValidator<UpdateBookRequest> _updateValidator;
    private readonly IValidator<UpdateReadingProgressRequest> _progressValidator;

    public BooksController(
        IBookService bookService,
        IValidator<CreateBookRequest> createValidator,
        IValidator<UpdateBookRequest> updateValidator,
        IValidator<UpdateReadingProgressRequest> progressValidator)
    {
        _bookService = bookService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _progressValidator = progressValidator;
    }

    // ────────────── PUBLIC ENDPOINTS ──────────────

    /// <summary>
    /// Browse all books with search, filter by genre, pagination, and sorting.
    /// 
    /// Query params:
    ///   ?search=harry        → Search by title or author
    ///   &amp;genre=Fantasy        → Filter by genre
    ///   &amp;sortBy=title        → Sort by: title, author, rating, date, favorites
    ///   &amp;descending=true     → Sort direction
    ///   &amp;page=1&amp;pageSize=20  → Pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<BookResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBooks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? genre = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = true)
    {
        var userId = GetUserId(); // null if not logged in
        var result = await _bookService.GetBooksAsync(page, pageSize, search, genre, sortBy, descending, userId);
        return Ok(ApiResponse<PaginatedResponse<BookResponse>>.Ok(result));
    }

    /// <summary>
    /// Get a single book by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<BookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBook(int id)
    {
        try
        {
            var userId = GetUserId();
            var book = await _bookService.GetBookByIdAsync(id, userId);
            return Ok(ApiResponse<BookResponse>.Ok(book));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail("Book not found.", 404));
        }
    }

    /// <summary>
    /// Get all available genres.
    /// </summary>
    [HttpGet("genres")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGenres()
    {
        var genres = await _bookService.GetGenresAsync();
        return Ok(ApiResponse<List<string>>.Ok(genres));
    }

    // ────────────── PROTECTED ENDPOINTS ──────────────

    /// <summary>
    /// Add a new book.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BookResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.", 400,
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var userId = GetRequiredUserId();
            var book = await _bookService.CreateBookAsync(request, userId);
            return StatusCode(201, ApiResponse<BookResponse>.Created(book, "Book added successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Update a book (only the user who added it can update).
    /// Partial update — only send fields you want to change.
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<BookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.", 400,
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var userId = GetRequiredUserId();
            var book = await _bookService.UpdateBookAsync(id, request, userId);
            return Ok(ApiResponse<BookResponse>.Ok(book, "Book updated successfully."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail("Book not found.", 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message, 403));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Delete a book (only the user who added it can delete).
    /// </summary>
    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBook(int id)
    {
        try
        {
            var userId = GetRequiredUserId();
            await _bookService.DeleteBookAsync(id, userId);
            return Ok(ApiResponse.Ok("Book deleted successfully."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail("Book not found.", 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message, 403));
        }
    }

    // ────────────── FAVORITES ──────────────

    /// <summary>
    /// Toggle favorite on a book (add if not favorited, remove if already favorited).
    /// </summary>
    [Authorize]
    [HttpPost("{id:int}/favorite")]
    [ProducesResponseType(typeof(ApiResponse<BookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        try
        {
            var userId = GetRequiredUserId();
            var book = await _bookService.ToggleFavoriteAsync(id, userId);
            var message = book.IsFavorited ? "Book added to favorites." : "Book removed from favorites.";
            return Ok(ApiResponse<BookResponse>.Ok(book, message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail("Book not found.", 404));
        }
    }

    /// <summary>
    /// Get my favorited books.
    /// </summary>
    [Authorize]
    [HttpGet("my/favorites")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<BookResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetRequiredUserId();
        var result = await _bookService.GetFavoritesAsync(userId, page, pageSize);
        return Ok(ApiResponse<PaginatedResponse<BookResponse>>.Ok(result));
    }

    // ────────────── READING PROGRESS ──────────────

    /// <summary>
    /// Update reading progress for a book.
    /// Status: 0=WantToRead, 1=Reading, 2=Finished, 3=Dropped
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}/progress")]
    [ProducesResponseType(typeof(ApiResponse<ReadingProgressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdateReadingProgressRequest request)
    {
        var validation = await _progressValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.", 400,
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var userId = GetRequiredUserId();
            var progress = await _bookService.UpdateReadingProgressAsync(id, request, userId);
            return Ok(ApiResponse<ReadingProgressResponse>.Ok(progress, "Reading progress updated."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail("Book not found.", 404));
        }
    }

    /// <summary>
    /// Get my reading library (books I've started tracking).
    /// Filter by status: ?status=1 (Reading), ?status=2 (Finished), etc.
    /// </summary>
    [Authorize]
    [HttpGet("my/library")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<BookResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLibrary(
        [FromQuery] int? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetRequiredUserId();
        var result = await _bookService.GetMyBooksAsync(userId, status, page, pageSize);
        return Ok(ApiResponse<PaginatedResponse<BookResponse>>.Ok(result));
    }

    // ────────────── HELPERS ──────────────

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && int.TryParse(claim, out var id) ? id : null;
    }

    private int GetRequiredUserId()
    {
        return GetUserId() ?? throw new UnauthorizedAccessException("User not authenticated.");
    }
}
