using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.StockIn.DTOs;
using VillageShop.Application.StockIn.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly IApplicationDbContext _context;

    public StockService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PostResponse> CreateAsync(CreateStockInRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return PostResponse.Error("Product / Item name is required.", 400);
        }

        long targetTenantId = request.TenantId ?? 0;
        if (targetTenantId <= 0 && !string.IsNullOrWhiteSpace(request.TenantCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == request.TenantCode.ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }
        if (targetTenantId <= 0) targetTenantId = 1;

        var product = new Stock
        {
            ItemId = request.ItemId,
            TenantId = targetTenantId,
            ProductCode = string.IsNullOrWhiteSpace(request.ProductCode) ? $"PRD-{Guid.NewGuid().ToString()[..6].ToUpper()}" : request.ProductCode,
            Name = request.Name,
            Category = request.Category,
            Brand = request.Brand,
            Unit = request.Unit ?? "pcs",
            Barcode = request.Barcode,
            PurchasePrice = request.PurchasePrice,
            SellingPrice = request.SellingPrice,
            MRP = request.MRP > 0 ? request.MRP : request.SellingPrice,
            GSTPercent = request.GSTPercent,
            OpeningStock = request.OpeningStock,
            MinimumStock = request.MinimumStock,
            CurrentStock = request.CurrentStock ?? request.OpeningStock,
            BatchNumber = request.BatchNumber,
            RackNumber = request.RackNumber,
            ExpiryDate = request.ExpiryDate,
            HSNCode = request.HSNCode,
            ImageUrl = (request.ImageUrl != null && request.ImageUrl.Length > 500000) ? "" : request.ImageUrl
        };

        _context.Stock.Add(product);
        await _context.SaveChangesAsync();

        return PostResponse.Success($"Stock entry '{product.Name}' created successfully.", product.ID);
    }

    public async Task<PostResponse> UpdateAsync(UpdateStockInRequest request)
    {
        long targetTenantId = request.TenantId ?? 0;
        if (targetTenantId <= 0 && !string.IsNullOrWhiteSpace(request.TenantCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == request.TenantCode.ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }
        if (targetTenantId <= 0) targetTenantId = 1;

        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => !p.IsDeleted &&
            ((request.ID > 0 && p.ID == request.ID) ||
             (request.ItemId.HasValue && request.ItemId > 0 && p.ItemId == request.ItemId.Value) ||
             (!string.IsNullOrWhiteSpace(request.Name) && p.Name.ToLower() == request.Name.ToLower() && p.TenantId == targetTenantId)));

        if (product == null)
        {
            // Item exists in V_Items catalog but doesn't have a V_Stock row yet; create new stock entry
            return await CreateAsync(request);
        }

        if (!string.IsNullOrWhiteSpace(request.Name)) product.Name = request.Name;
        if (request.ItemId.HasValue && request.ItemId > 0) product.ItemId = request.ItemId;
        if (!string.IsNullOrWhiteSpace(request.Category)) product.Category = request.Category;
        if (!string.IsNullOrWhiteSpace(request.Brand)) product.Brand = request.Brand;
        if (!string.IsNullOrWhiteSpace(request.Unit)) product.Unit = request.Unit;
        if (!string.IsNullOrWhiteSpace(request.Barcode)) product.Barcode = request.Barcode;
        if (request.PurchasePrice > 0) product.PurchasePrice = request.PurchasePrice;
        if (request.SellingPrice > 0) product.SellingPrice = request.SellingPrice;
        if (request.MRP > 0) product.MRP = request.MRP;
        if (request.GSTPercent > 0) product.GSTPercent = request.GSTPercent;
        if (request.CurrentStock.HasValue) product.CurrentStock = request.CurrentStock.Value;
        if (request.MinimumStock > 0) product.MinimumStock = request.MinimumStock;
        if (!string.IsNullOrWhiteSpace(request.BatchNumber)) product.BatchNumber = request.BatchNumber;
        if (!string.IsNullOrWhiteSpace(request.RackNumber)) product.RackNumber = request.RackNumber;
        if (request.ExpiryDate.HasValue) product.ExpiryDate = request.ExpiryDate;
        if (!string.IsNullOrWhiteSpace(request.HSNCode)) product.HSNCode = request.HSNCode;
        if (!string.IsNullOrWhiteSpace(request.ImageUrl)) product.ImageUrl = request.ImageUrl.Length > 500000 ? "" : request.ImageUrl;

        await _context.SaveChangesAsync();
        return PostResponse.Success($"Stock item '{product.Name}' updated successfully.", product.ID);
    }

    public async Task<PostResponse> DeleteAsync(long id)
    {
        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ID == id && !p.IsDeleted);
        if (product == null)
        {
            return PostResponse.Error("Stock item not found.", 404);
        }

        product.IsDeleted = true;
        product.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return PostResponse.Success("Stock item deleted successfully.", id);
    }

    public async Task<Stock?> GetByIdAsync(long id)
    {
        return await _context.Stock.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ID == id && !p.IsDeleted);
    }

    public async Task<IEnumerable<Stock>> SearchAsync(StockInSearchRequest request)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 200 : request.PageSize;

        long targetTenantId = request.TenantId ?? 0;
        if (targetTenantId <= 0 && !string.IsNullOrWhiteSpace(request.TenantCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == request.TenantCode.ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }

        // Explicit LINQ Join between V_Stock and V_Items
        var stockQuery = _context.Stock.IgnoreQueryFilters().AsNoTracking().Where(p => !p.IsDeleted);
        var itemsQuery = _context.Items.IgnoreQueryFilters().AsNoTracking().Where(i => !i.IsDeleted);

        if (targetTenantId > 0)
        {
            stockQuery = stockQuery.Where(p => p.TenantId == targetTenantId);
            itemsQuery = itemsQuery.Where(i => i.TenantId == targetTenantId);
        }

        // Perform Left Join of Stock with Items on ItemId == Item.ID or ProductCode == ItemCode
        var joinedQuery = from s in stockQuery
                          join i in itemsQuery on s.ItemId equals i.ID into itemGroup
                          from i in itemGroup.DefaultIfEmpty()
                          select new Stock
                          {
                              ID = s.ID,
                              TenantId = s.TenantId,
                              ItemId = s.ItemId ?? (i != null ? (long?)i.ID : null),
                              ProductCode = s.ProductCode,
                              Name = i != null && !string.IsNullOrWhiteSpace(i.Name) ? i.Name : s.Name,
                              Category = i != null && !string.IsNullOrWhiteSpace(i.Category) ? i.Category : s.Category,
                              Brand = s.Brand,
                              Unit = i != null && !string.IsNullOrWhiteSpace(i.Unit) ? i.Unit : s.Unit,
                              Barcode = s.Barcode,
                              PurchasePrice = s.PurchasePrice,
                              SellingPrice = s.SellingPrice,
                              MRP = s.MRP,
                              GSTPercent = s.GSTPercent,
                              OpeningStock = s.OpeningStock,
                              MinimumStock = s.MinimumStock,
                              CurrentStock = s.CurrentStock,
                              BatchNumber = s.BatchNumber,
                              RackNumber = s.RackNumber,
                              ExpiryDate = s.ExpiryDate,
                              HSNCode = s.HSNCode,
                              ImageUrl = s.ImageUrl,
                              IsActive = s.IsActive,
                              Priority = s.Priority,
                              CreatedBy = s.CreatedBy,
                              CreatedDate = s.CreatedDate,
                              ModifiedBy = s.ModifiedBy,
                              ModifiedDate = s.ModifiedDate
                          };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            joinedQuery = joinedQuery.Where(p => (p.Name != null && p.Name.ToLower().Contains(searchLower)) ||
                                                 (p.Barcode != null && p.Barcode.ToLower().Contains(searchLower)) ||
                                                 (p.BatchNumber != null && p.BatchNumber.ToLower().Contains(searchLower)) ||
                                                 (p.RackNumber != null && p.RackNumber.ToLower().Contains(searchLower)) ||
                                                 (p.ProductCode != null && p.ProductCode.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            joinedQuery = joinedQuery.Where(p => p.Category == request.Category);
        }

        var stockList = await joinedQuery
            .OrderByDescending(p => p.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Include catalog items from V_Items that don't have stock entries yet
        try
        {
            var existingNames = stockList.Select(s => (s.Name ?? "").ToLower()).ToHashSet();
            var existingItemIds = stockList.Select(s => s.ItemId ?? 0).Where(id => id > 0).ToHashSet();
            var existingCodes = stockList.Select(s => (s.ProductCode ?? "").ToLower()).ToHashSet();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var sLower = request.Search.ToLower();
                itemsQuery = itemsQuery.Where(i => (i.Name != null && i.Name.ToLower().Contains(sLower)) ||
                                                   (i.ItemCode != null && i.ItemCode.ToLower().Contains(sLower)) ||
                                                   (i.Category != null && i.Category.ToLower().Contains(sLower)));
            }

            var unlinkedItems = await itemsQuery.ToListAsync();
            foreach (var item in unlinkedItems)
            {
                var nameLower = (item.Name ?? "").ToLower();
                var codeLower = (item.ItemCode ?? "").ToLower();
                if (!existingItemIds.Contains(item.ID) && !existingNames.Contains(nameLower) && !existingCodes.Contains(codeLower))
                {
                    stockList.Add(new Stock
                    {
                        ID = item.ID,
                        TenantId = item.TenantId,
                        ItemId = item.ID,
                        ProductCode = item.ItemCode ?? $"ITM-{item.ID}",
                        Name = item.Name ?? "Item",
                        Category = item.Category,
                        Unit = string.IsNullOrWhiteSpace(item.Unit) ? "pcs" : item.Unit,
                        Barcode = item.ItemCode,
                        PurchasePrice = 0,
                        SellingPrice = 0,
                        MRP = 0,
                        GSTPercent = 0,
                        OpeningStock = 0,
                        MinimumStock = 0,
                        CurrentStock = 0,
                        ImageUrl = ""
                    });
                }
            }
        }
        catch (Exception) {}

        foreach (var stock in stockList)
        {
            if (stock.ImageUrl != null && stock.ImageUrl.Length > 500000)
            {
                stock.ImageUrl = "";
            }
        }

        return stockList;
    }
}
