using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.Items.DTOs;
using VillageShop.Application.Items.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Infrastructure.Services;

public class ItemService : IItemService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;

    public ItemService(IApplicationDbContext context, ICurrentTenantService currentTenantService)
    {
        _context = context;
        _currentTenantService = currentTenantService;
    }

    public async Task<PostResponse> CreateAsync(CreateItemRequest request)
    {
        try
        {
            long effectiveTenantId = request.TenantId ?? _currentTenantService.TenantId;
            if (effectiveTenantId <= 0) effectiveTenantId = 1;

            var rawName = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : request.ItemName;
            if (string.IsNullOrWhiteSpace(rawName)) rawName = $"Item #{Random.Shared.Next(100, 999)}";

            var categoryStr = request.Category;
            long? catId = request.CategoryId;
            if (catId.HasValue && catId.Value > 0 && string.IsNullOrWhiteSpace(categoryStr))
            {
                var catMaster = await _context.Masters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ID == catId.Value && !m.IsDeleted);
                if (catMaster != null) categoryStr = catMaster.MasterName;
            }
            else if (!string.IsNullOrWhiteSpace(categoryStr) && (!catId.HasValue || catId.Value <= 0))
            {
                var catMaster = await _context.Masters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.TenantId == effectiveTenantId && m.MasterType.ToLower() == "itemcategory" && m.MasterName.ToLower() == categoryStr.Trim().ToLower() && !m.IsDeleted);
                if (catMaster != null) catId = catMaster.ID;
            }

            var unitStr = string.IsNullOrWhiteSpace(request.Unit) ? "pcs" : request.Unit.Trim();
            long? uId = request.UnitId;
            if (uId.HasValue && uId.Value > 0 && string.IsNullOrWhiteSpace(request.Unit))
            {
                var uomMaster = await _context.Masters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ID == uId.Value && !m.IsDeleted);
                if (uomMaster != null) unitStr = uomMaster.MasterName;
            }

            var formatStr = string.IsNullOrWhiteSpace(request.Format) ? "Packed" : request.Format.Trim();
            long? typeId = request.ItemTypeId;
            if (typeId.HasValue && typeId.Value > 0 && string.IsNullOrWhiteSpace(request.Format))
            {
                var typeMaster = await _context.Masters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ID == typeId.Value && !m.IsDeleted);
                if (typeMaster != null) formatStr = typeMaster.MasterName;
            }

            var item = new Item
            {
                TenantId = effectiveTenantId,
                ItemCode = string.IsNullOrWhiteSpace(request.ItemCode) ? $"ITM-{Random.Shared.Next(1000, 9999)}" : request.ItemCode.Trim(),
                Name = rawName.Trim(),
                Category = categoryStr,
                CategoryId = catId,
                Unit = unitStr,
                UnitId = uId,
                Format = formatStr,
                ItemTypeId = typeId,
                BrandId = request.BrandId,
                Description = request.Description,
                IsActive = true,
                IsDeleted = false
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return PostResponse.Success("Item record created successfully.", item.ID);
        }
        catch (Exception ex)
        {
            return PostResponse.Error($"Failed to create Item: {ex.Message}");
        }
    }

    public async Task<PostResponse> UpdateAsync(UpdateItemRequest request)
    {
        try
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.ID == request.ID && !i.IsDeleted);
            if (item == null)
            {
                return PostResponse.Error("Item not found.", 404);
            }

            if (!string.IsNullOrWhiteSpace(request.ItemCode)) item.ItemCode = request.ItemCode.Trim();
            if (!string.IsNullOrWhiteSpace(request.Name)) item.Name = request.Name.Trim();
            if (request.Category != null) item.Category = request.Category;
            if (request.CategoryId.HasValue) item.CategoryId = request.CategoryId.Value;
            if (item.CategoryId.HasValue && item.CategoryId.Value > 0 && string.IsNullOrWhiteSpace(item.Category))
            {
                var catMaster = await _context.Masters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ID == item.CategoryId.Value && !m.IsDeleted);
                if (catMaster != null) item.Category = catMaster.MasterName;
            }
            if (!string.IsNullOrWhiteSpace(request.Unit)) item.Unit = request.Unit.Trim();
            if (request.UnitId.HasValue) item.UnitId = request.UnitId.Value;
            if (!string.IsNullOrWhiteSpace(request.Format)) item.Format = request.Format.Trim();
            if (request.ItemTypeId.HasValue) item.ItemTypeId = request.ItemTypeId.Value;
            if (request.BrandId.HasValue) item.BrandId = request.BrandId.Value;
            if (request.Description != null) item.Description = request.Description;

            await _context.SaveChangesAsync();
            return PostResponse.Success("Item record updated successfully.", item.ID);
        }
        catch (Exception ex)
        {
            return PostResponse.Error($"Failed to update Item: {ex.Message}");
        }
    }

    public async Task<PostResponse> DeleteAsync(long id)
    {
        try
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.ID == id && !i.IsDeleted);
            if (item == null)
            {
                return PostResponse.Error("Item not found.", 404);
            }

            item.IsDeleted = true;
            await _context.SaveChangesAsync();
            return PostResponse.Success("Item deleted successfully.", id);
        }
        catch (Exception ex)
        {
            return PostResponse.Error($"Failed to delete Item: {ex.Message}");
        }
    }

    public async Task<Item?> GetByIdAsync(long id)
    {
        return await _context.Items.FirstOrDefaultAsync(i => i.ID == id && !i.IsDeleted);
    }

    private async Task<(IQueryable<Item> Query, long TenantId)> BuildSearchQueryAsync(ItemSearchRequest request)
    {
        var query = _context.Items.IgnoreQueryFilters().AsNoTracking().Where(i => !i.IsDeleted);

        long effectiveTenantId = request.TenantId ?? 0;
        if (effectiveTenantId <= 0 && !string.IsNullOrWhiteSpace(request.TenantCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == request.TenantCode.ToLower() && !t.IsDeleted);
            if (tenant != null) effectiveTenantId = tenant.ID;
        }

        if (effectiveTenantId <= 0 && _currentTenantService != null && _currentTenantService.TenantId > 0)
        {
            effectiveTenantId = _currentTenantService.TenantId;
        }

        if (effectiveTenantId > 0)
        {
            query = query.Where(i => i.TenantId == effectiveTenantId);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower().Trim();
            query = query.Where(i => (i.Name != null && i.Name.ToLower().Contains(term)) ||
                                     (i.ItemCode != null && i.ItemCode.ToLower().Contains(term)) ||
                                     (i.Category != null && i.Category.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var cat = request.Category.Trim().ToLower();
            query = query.Where(i => i.Category != null && i.Category.ToLower() == cat);
        }

        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            query = query.Where(i => i.CategoryId == request.CategoryId.Value);
        }

        return (query, effectiveTenantId);
    }

    private static IQueryable<Item> ApplySorting(IQueryable<Item> query, string? sortBy, string? sortDirection)
    {
        bool isAsc = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.Trim().ToLower()) switch
        {
            "name" => isAsc ? query.OrderBy(i => i.Name).ThenByDescending(i => i.ID) : query.OrderByDescending(i => i.Name).ThenByDescending(i => i.ID),
            "itemcode" => isAsc ? query.OrderBy(i => i.ItemCode).ThenByDescending(i => i.ID) : query.OrderByDescending(i => i.ItemCode).ThenByDescending(i => i.ID),
            "category" => isAsc ? query.OrderBy(i => i.Category).ThenByDescending(i => i.ID) : query.OrderByDescending(i => i.Category).ThenByDescending(i => i.ID),
            "unit" => isAsc ? query.OrderBy(i => i.Unit).ThenByDescending(i => i.ID) : query.OrderByDescending(i => i.Unit).ThenByDescending(i => i.ID),
            _ => isAsc ? query.OrderBy(i => i.ID) : query.OrderByDescending(i => i.ID),
        };
    }

    public async Task<IEnumerable<Item>> SearchAsync(ItemSearchRequest request)
    {
        var (query, _) = await BuildSearchQueryAsync(request);
        var sortedQuery = ApplySorting(query, request.SortBy, request.SortDirection);

        if (request.PageSize > 0)
        {
            int page = request.Page < 1 ? 1 : request.Page;
            int pageSize = Math.Min(request.PageSize, 200);
            return await sortedQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        return await sortedQuery.Take(200).ToListAsync();
    }

    public async Task<PagedResult<Item>> SearchPagedAsync(ItemSearchRequest request)
    {
        var (query, _) = await BuildSearchQueryAsync(request);
        int totalCount = await query.CountAsync();

        var sortedQuery = ApplySorting(query, request.SortBy, request.SortDirection);
        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 200);

        var items = await sortedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<Item>.Create(items, totalCount, page, pageSize);
    }

    public async Task<BulkImportResult> BulkImportAsync(BulkImportRequest request)
    {
        var result = new BulkImportResult
        {
            TotalRows = request.Items.Count
        };

        if (request.Items == null || request.Items.Count == 0)
        {
            return result;
        }

        long effectiveTenantId = request.TenantId ?? 0;
        if (effectiveTenantId <= 0 && !string.IsNullOrWhiteSpace(request.TenantCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == request.TenantCode.ToLower() && !t.IsDeleted);
            if (tenant != null) effectiveTenantId = tenant.ID;
        }
        if (effectiveTenantId <= 0 && _currentTenantService != null && _currentTenantService.TenantId > 0)
        {
            effectiveTenantId = _currentTenantService.TenantId;
        }
        if (effectiveTenantId <= 0) effectiveTenantId = 1;

        // Process in chunks of 100 to avoid high memory spikes
        const int batchSize = 100;
        for (int i = 0; i < request.Items.Count; i += batchSize)
        {
            var batch = request.Items.Skip(i).Take(batchSize).ToList();
            var newItems = new List<Item>();
            var newStocks = new List<Stock>();

            foreach (var row in batch)
            {
                int rowIdx = row.RowIndex > 0 ? row.RowIndex : (i + newItems.Count + 1);

                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkImportRowError
                    {
                        RowIndex = rowIdx,
                        Field = "Name",
                        ErrorMessage = "Item Name is required."
                    });
                    continue;
                }

                var itemCode = string.IsNullOrWhiteSpace(row.ItemCode)
                    ? $"ITM-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
                    : row.ItemCode.Trim();

                var item = new Item
                {
                    TenantId = effectiveTenantId,
                    ItemCode = itemCode,
                    Name = row.Name.Trim(),
                    Category = string.IsNullOrWhiteSpace(row.Category) ? "General" : row.Category.Trim(),
                    Unit = string.IsNullOrWhiteSpace(row.Unit) ? "pcs" : row.Unit.Trim(),
                    Format = string.IsNullOrWhiteSpace(row.Format) ? "Packed" : row.Format.Trim(),
                    Description = row.Description,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedBy = 1,
                    EntrySource = "BulkImport",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                newItems.Add(item);

                // If stock or pricing info is provided, create linked stock
                if (row.PurchasePrice.HasValue || row.SellingPrice.HasValue || row.MRP.HasValue || row.OpeningStock.HasValue || !string.IsNullOrWhiteSpace(row.Barcode))
                {
                    newStocks.Add(new Stock
                    {
                        TenantId = effectiveTenantId,
                        ProductCode = itemCode,
                        Name = item.Name,
                        Category = item.Category,
                        Brand = row.Brand ?? "",
                        Unit = item.Unit,
                        Barcode = string.IsNullOrWhiteSpace(row.Barcode) ? $"890{Random.Shared.Next(100000000, 999999999)}" : row.Barcode.Trim(),
                        PurchasePrice = row.PurchasePrice ?? 0m,
                        SellingPrice = row.SellingPrice ?? 0m,
                        MRP = row.MRP ?? (row.SellingPrice ?? 0m),
                        OpeningStock = row.OpeningStock ?? 0m,
                        CurrentStock = row.OpeningStock ?? 0m,
                        MinimumStock = row.MinimumStock ?? 5m,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedBy = 1,
                        EntrySource = "BulkImport",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });
                }
            }

            try
            {
                if (newItems.Count > 0)
                {
                    await _context.Items.AddRangeAsync(newItems);
                    await _context.SaveChangesAsync();

                    // Link stock items to saved items
                    for (int sIdx = 0; sIdx < newStocks.Count; sIdx++)
                    {
                        var matchingItem = newItems.FirstOrDefault(item => item.ItemCode == newStocks[sIdx].ProductCode);
                        if (matchingItem != null)
                        {
                            newStocks[sIdx].ItemId = matchingItem.ID;
                        }
                    }

                    if (newStocks.Count > 0)
                    {
                        await _context.Stock.AddRangeAsync(newStocks);
                        await _context.SaveChangesAsync();
                    }

                    result.SuccessCount += newItems.Count;
                }
            }
            catch (Exception ex)
            {
                result.FailureCount += newItems.Count;
                result.Errors.Add(new BulkImportRowError
                {
                    RowIndex = i + 1,
                    Field = "Batch",
                    ErrorMessage = $"Batch save failed: {ex.Message}"
                });
            }
        }

        return result;
    }
}

