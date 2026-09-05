// <copyright file="PaginatedList.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
        this.Items = items;
        this.PageNumber = Math.Max(pageNumber, 1);
        this.PageSize = Math.Clamp(pageSize, 1, 50);
        this.TotalCount = count;
        this.TotalPages = (int)Math.Ceiling(this.TotalCount / (double)this.PageSize);
    }

    public bool HasPreviousPage => this.PageNumber > 1;

    public bool HasNextPage => this.PageNumber < this.TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        int count = await source.CountAsync();
        IReadOnlyCollection<T> items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PaginatedList<T>(items, pageNumber, pageSize, count);
    }
}
