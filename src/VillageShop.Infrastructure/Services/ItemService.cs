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

    public async Task<IEnumerable<Item>> SearchAsync(ItemSearchRequest request)
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

        return await query.OrderByDescending(i => i.ID).ToListAsync();
    }
}
