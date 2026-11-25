namespace MyApi.Models.DTOs;

/// <summary>
/// Paginated response — used for listing books.
/// 
/// In Flutter, you'd use this for infinite scroll / "load more":
///   - First call: /api/books?page=1&pageSize=20
///   - Load more:  /api/books?page=2&pageSize=20
///   - Until hasNextPage == false
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
