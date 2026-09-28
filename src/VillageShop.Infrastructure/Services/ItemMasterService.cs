using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.ItemMasters.DTOs;
using VillageShop.Application.ItemMasters.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Infrastructure.Services;

public class ItemMasterService : IItemMasterService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;

    public ItemMasterService(IApplicationDbContext context, ICurrentTenantService currentTenantService)
    {
        _context = context;
        _currentTenantService = currentTenantService;
    }

    public async Task<PostResponse> CreateAsync(CreateItemMasterRequest request)
    {
        try
        {
            long effectiveTenantId = request.TenantId ?? _currentTenantService.TenantId;
            if (effectiveTenantId <= 0) effectiveTenantId = 1;

            var item = new ItemMaster
            {
                TenantId = effectiveTenantId,
                ItemCode = string.IsNullOrWhiteSpace(request.ItemCode) ? $"ITM-{Random.Shared.Next(1000, 9999)}" : request.ItemCode.Trim(),
                Name = request.Name.Trim(),
                Category = request.Category,
                Unit = string.IsNullOrWhiteSpace(request.Unit) ? "pcs" : request.Unit.Trim(),
                Format = string.IsNullOrWhiteSpace(request.Format) ? "Packed" : request.Format.Trim(),
                Description = request.Description,
                IsActive = true,
                IsDeleted = false
            };

            _context.ItemMasters.Add(item);
            await _context.SaveChangesAsync();

            return PostResponse.Success("Item Master record created successfully.", item.ID);
        }
        catch (Exception ex)
        {
            return PostResponse.Error($"Failed to create Item Master: {ex.Message}");
        }
    }

    public async Task<PostResponse> UpdateAsync(UpdateItemMasterRequest request)
    {
        try
        {
            var item = await _context.ItemMasters.FirstOrDefaultAsync(i => i.ID == request.ID && !i.IsDeleted);
            if (item == null)
            {
                return PostResponse.Error("Item Master not found.", 404);
            }

            if (!string.IsNullOrWhiteSpace(request.ItemCode)) item.ItemCode = request.ItemCode.Trim();
            if (!string.IsNullOrWhiteSpace(request.Name)) item.Name = request.Name.Trim();
            if (request.Category != null) item.Category = request.Category;
            if (!string.IsNullOrWhiteSpace(request.Unit)) item.Unit = request.Unit.Trim();
            if (!string.IsNullOrWhiteSpace(request.Format)) item.Format = request.Format.Trim();
            if (request.Description != null) item.Description = request.Description;

            await _context.SaveChangesAsync();
            return PostResponse.Success("Item Master record updated successfully.", item.ID);
        }
        catch (Exception ex)
        {
            return PostResponse.Error($"Failed to update Item Master: {ex.Message}");
        }
    }

    public async Task<PostResponse> DeleteAsync(long id)
    {
        try
        {
            var item = await _context.ItemMasters.FirstOrDefaultAsync(i => i.ID == id && !i.IsDeleted);
            if (item == null)
            {
                return PostResponse.Error("Item Master not found.", 404);
            }

            item.IsDeleted = true;
            await _context.SaveChangesAsync();
            return PostResponse.Success("Item Master deleted successfully.", id);
        }
        catch (Exception ex)
        {
            return PostResponse.Error($"Failed to delete Item Master: {ex.Message}");
        }
    }

    public async Task<ItemMaster?> GetByIdAsync(long id)
    {
        return await _context.ItemMasters.FirstOrDefaultAsync(i => i.ID == id && !i.IsDeleted);
    }

    public async Task<IEnumerable<ItemMaster>> SearchAsync(ItemMasterSearchRequest request)
    {
        var query = _context.ItemMasters.Where(i => !i.IsDeleted);

        long effectiveTenantId = request.TenantId ?? _currentTenantService.TenantId;
        if (effectiveTenantId > 0)
        {
            query = query.Where(i => i.TenantId == effectiveTenantId);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower().Trim();
            query = query.Where(i => i.Name.ToLower().Contains(term) ||
                                     i.ItemCode.ToLower().Contains(term) ||
                                     (i.Category != null && i.Category.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(i => i.ID).ToListAsync();
    }
}
