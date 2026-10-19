using System;
using System.Collections.Generic;

namespace VillageShop.Common.Models;

public class PagedRequest
{
    private int _page = 1;
    private int _pageSize = 25;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageNumber
    {
        get => Page;
        set => Page = value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 200 ? 200 : (value < 1 ? 25 : value);
    }

    public string? Search { get; set; }

    public string? SearchTerm
    {
        get => Search;
        set => Search = value;
    }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; } = "desc";

    public long? TenantId { get; set; }

    public string? TenantCode { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();

    public int Page { get; set; } = 1;

    public int PageNumber
    {
        get => Page;
        set => Page = value;
    }

    public int PageSize { get; set; } = 25;

    public int TotalCount { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public bool HasMore => Page * PageSize < TotalCount;

    public bool HasNextPage => HasMore;

    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Create(List<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? 25 : pageSize
        };
    }
}
