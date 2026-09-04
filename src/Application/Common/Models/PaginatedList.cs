using Microsoft.EntityFrameworkCore;

namespace Application.Common.Models;

public class PaginatedList<T>
{

    public int PageNumber { get; private set; }
    public int PageSize { get; private init; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    public IReadOnlyCollection<T>? Items { get; init; }
    public PaginatedList(IReadOnlyCollection<T>? items, int pageNumber, int pageSize, int count)
    {
        Items = items;
        PageNumber = Math.Max(pageNumber, 1);
        PageSize = Math.Clamp(pageSize, 1, 50);
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        int count = await source.CountAsync();
        IReadOnlyCollection<T> items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PaginatedList<T>(items, pageNumber, pageSize, count);
    }
}
