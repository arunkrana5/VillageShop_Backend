using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class MastersController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public MastersController(IApplicationDbContext context)
    {
        _context = context;
    }

    private long ResolveTenantId(long? tenantId, string? tenantCode)
    {
        long targetTenantId = tenantId ?? 0;
        if (targetTenantId <= 0 && !string.IsNullOrWhiteSpace(tenantCode))
        {
            var tenant = _context.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.TenantCode.ToLower() == tenantCode.ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }

        if (targetTenantId <= 0 && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            targetTenantId = headerTid;
        }

        if (targetTenantId <= 0 && Request.Headers.TryGetValue("X-Tenant-Code", out var headerTCode) && !string.IsNullOrWhiteSpace(headerTCode))
        {
            var tenant = _context.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.TenantCode.ToLower() == headerTCode.ToString().ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }

        if (targetTenantId <= 0) targetTenantId = 1;
        return targetTenantId;
    }

    // =========================================
    // 1. CATEGORY MASTERS
    // =========================================

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);
        var query = _context.ItemCategories.IgnoreQueryFilters().AsNoTracking().Where(c => c.TenantId == tId && !c.IsDeleted);

        if (activeOnly) query = query.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c => c.CategoryName.ToLower().Contains(s) || (c.CategoryCode != null && c.CategoryCode.ToLower().Contains(s)));
        }

        var list = await query.OrderBy(c => c.Priority).ThenBy(c => c.CategoryName).ToListAsync();
        return Ok(list);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.CategoryName))
            return BadRequest(PostResponse.Error("Category Name is required."));

        long tId = ResolveTenantId(req.TenantId, req.TenantCode);

        bool exists = await _context.ItemCategories.IgnoreQueryFilters().AnyAsync(c => c.TenantId == tId && c.CategoryName.ToLower() == req.CategoryName.Trim().ToLower() && !c.IsDeleted);
        if (exists) return BadRequest(PostResponse.Error($"Category '{req.CategoryName}' already exists."));

        var entity = new ItemCategory
        {
            TenantId = tId,
            CategoryName = req.CategoryName.Trim(),
            CategoryCode = string.IsNullOrWhiteSpace(req.CategoryCode) ? $"CAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}" : req.CategoryCode.Trim(),
            Description = req.Description,
            Priority = req.Priority ?? 0,
            IsActive = true,
            IsDeleted = false
        };

        _context.ItemCategories.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success("Category created successfully.", entity.ID));
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] CategoryRequest req)
    {
        var entity = await _context.ItemCategories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Category not found.", 404));

        if (!string.IsNullOrWhiteSpace(req.CategoryName)) entity.CategoryName = req.CategoryName.Trim();
        if (req.CategoryCode != null) entity.CategoryCode = req.CategoryCode.Trim();
        if (req.Description != null) entity.Description = req.Description;
        if (req.Priority.HasValue) entity.Priority = req.Priority.Value;

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Category updated successfully.", entity.ID));
    }

    [HttpPut("categories/{id}/toggle-active")]
    public async Task<IActionResult> ToggleCategoryActive(long id)
    {
        var entity = await _context.ItemCategories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Category not found.", 404));

        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success($"Category is now {(entity.IsActive ? "Active" : "Inactive")}.", entity.ID));
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(long id)
    {
        var entity = await _context.ItemCategories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Category not found.", 404));

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Category deleted successfully.", entity.ID));
    }

    // =========================================
    // 2. UOM MASTERS (UNITS OF MEASUREMENT)
    // =========================================

    [HttpGet("uoms")]
    public async Task<IActionResult> GetUOMs([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);
        var query = _context.UnitOfMeasurements.IgnoreQueryFilters().AsNoTracking().Where(u => u.TenantId == tId && !u.IsDeleted);

        if (activeOnly) query = query.Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u => u.UOMName.ToLower().Contains(s) || u.UOMCode.ToLower().Contains(s) || u.Symbol.ToLower().Contains(s));
        }

        var list = await query.OrderBy(u => u.Priority).ThenBy(u => u.UOMName).ToListAsync();
        return Ok(list);
    }

    [HttpPost("uoms")]
    public async Task<IActionResult> CreateUOM([FromBody] UomRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.UOMName))
            return BadRequest(PostResponse.Error("UOM Name is required."));

        long tId = ResolveTenantId(req.TenantId, req.TenantCode);

        bool exists = await _context.UnitOfMeasurements.IgnoreQueryFilters().AnyAsync(u => u.TenantId == tId && u.UOMName.ToLower() == req.UOMName.Trim().ToLower() && !u.IsDeleted);
        if (exists) return BadRequest(PostResponse.Error($"UOM '{req.UOMName}' already exists."));

        var entity = new UnitOfMeasurement
        {
            TenantId = tId,
            UOMName = req.UOMName.Trim(),
            UOMCode = string.IsNullOrWhiteSpace(req.UOMCode) ? req.UOMName.Trim().ToUpper() : req.UOMCode.Trim().ToUpper(),
            Symbol = string.IsNullOrWhiteSpace(req.Symbol) ? req.UOMName.Trim().ToLower() : req.Symbol.Trim(),
            DecimalPrecision = req.DecimalPrecision ?? 0,
            Description = req.Description,
            Priority = req.Priority ?? 0,
            IsActive = true,
            IsDeleted = false
        };

        _context.UnitOfMeasurements.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success("UOM created successfully.", entity.ID));
    }

    [HttpPut("uoms/{id}")]
    public async Task<IActionResult> UpdateUOM(long id, [FromBody] UomRequest req)
    {
        var entity = await _context.UnitOfMeasurements.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.ID == id && !u.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("UOM not found.", 404));

        if (!string.IsNullOrWhiteSpace(req.UOMName)) entity.UOMName = req.UOMName.Trim();
        if (!string.IsNullOrWhiteSpace(req.UOMCode)) entity.UOMCode = req.UOMCode.Trim().ToUpper();
        if (!string.IsNullOrWhiteSpace(req.Symbol)) entity.Symbol = req.Symbol.Trim();
        if (req.DecimalPrecision.HasValue) entity.DecimalPrecision = req.DecimalPrecision.Value;
        if (req.Description != null) entity.Description = req.Description;
        if (req.Priority.HasValue) entity.Priority = req.Priority.Value;

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("UOM updated successfully.", entity.ID));
    }

    [HttpPut("uoms/{id}/toggle-active")]
    public async Task<IActionResult> ToggleUOMActive(long id)
    {
        var entity = await _context.UnitOfMeasurements.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.ID == id && !u.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("UOM not found.", 404));

        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success($"UOM is now {(entity.IsActive ? "Active" : "Inactive")}.", entity.ID));
    }

    [HttpDelete("uoms/{id}")]
    public async Task<IActionResult> DeleteUOM(long id)
    {
        var entity = await _context.UnitOfMeasurements.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.ID == id && !u.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("UOM not found.", 404));

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("UOM deleted successfully.", entity.ID));
    }

    // =========================================
    // 3. ITEM TYPE MASTERS
    // =========================================

    [HttpGet("item-types")]
    public async Task<IActionResult> GetItemTypes([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);
        var query = _context.ItemTypes.IgnoreQueryFilters().AsNoTracking().Where(t => t.TenantId == tId && !t.IsDeleted);

        if (activeOnly) query = query.Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(t => t.ItemTypeName.ToLower().Contains(s));
        }

        var list = await query.OrderBy(t => t.Priority).ThenBy(t => t.ItemTypeName).ToListAsync();
        return Ok(list);
    }

    [HttpPost("item-types")]
    public async Task<IActionResult> CreateItemType([FromBody] ItemTypeRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.ItemTypeName))
            return BadRequest(PostResponse.Error("Item Type Name is required."));

        long tId = ResolveTenantId(req.TenantId, req.TenantCode);

        bool exists = await _context.ItemTypes.IgnoreQueryFilters().AnyAsync(t => t.TenantId == tId && t.ItemTypeName.ToLower() == req.ItemTypeName.Trim().ToLower() && !t.IsDeleted);
        if (exists) return BadRequest(PostResponse.Error($"Item Type '{req.ItemTypeName}' already exists."));

        var entity = new ItemType
        {
            TenantId = tId,
            ItemTypeName = req.ItemTypeName.Trim(),
            ItemTypeCode = string.IsNullOrWhiteSpace(req.ItemTypeCode) ? req.ItemTypeName.Trim().ToUpper() : req.ItemTypeCode.Trim().ToUpper(),
            Description = req.Description,
            Priority = req.Priority ?? 0,
            IsActive = true,
            IsDeleted = false
        };

        _context.ItemTypes.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success("Item Type created successfully.", entity.ID));
    }

    [HttpPut("item-types/{id}")]
    public async Task<IActionResult> UpdateItemType(long id, [FromBody] ItemTypeRequest req)
    {
        var entity = await _context.ItemTypes.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.ID == id && !t.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Item Type not found.", 404));

        if (!string.IsNullOrWhiteSpace(req.ItemTypeName)) entity.ItemTypeName = req.ItemTypeName.Trim();
        if (req.ItemTypeCode != null) entity.ItemTypeCode = req.ItemTypeCode.Trim().ToUpper();
        if (req.Description != null) entity.Description = req.Description;
        if (req.Priority.HasValue) entity.Priority = req.Priority.Value;

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Item Type updated successfully.", entity.ID));
    }

    [HttpPut("item-types/{id}/toggle-active")]
    public async Task<IActionResult> ToggleItemTypeActive(long id)
    {
        var entity = await _context.ItemTypes.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.ID == id && !t.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Item Type not found.", 404));

        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success($"Item Type is now {(entity.IsActive ? "Active" : "Inactive")}.", entity.ID));
    }

    [HttpDelete("item-types/{id}")]
    public async Task<IActionResult> DeleteItemType(long id)
    {
        var entity = await _context.ItemTypes.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.ID == id && !t.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Item Type not found.", 404));

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Item Type deleted successfully.", entity.ID));
    }

    // =========================================
    // 4. BRAND MASTERS
    // =========================================

    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);
        var query = _context.Brands.IgnoreQueryFilters().AsNoTracking().Where(b => b.TenantId == tId && !b.IsDeleted);

        if (activeOnly) query = query.Where(b => b.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(b => b.BrandName.ToLower().Contains(s));
        }

        var list = await query.OrderBy(b => b.Priority).ThenBy(b => b.BrandName).ToListAsync();
        return Ok(list);
    }

    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand([FromBody] BrandRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.BrandName))
            return BadRequest(PostResponse.Error("Brand Name is required."));

        long tId = ResolveTenantId(req.TenantId, req.TenantCode);

        bool exists = await _context.Brands.IgnoreQueryFilters().AnyAsync(b => b.TenantId == tId && b.BrandName.ToLower() == req.BrandName.Trim().ToLower() && !b.IsDeleted);
        if (exists) return BadRequest(PostResponse.Error($"Brand '{req.BrandName}' already exists."));

        var entity = new Brand
        {
            TenantId = tId,
            BrandName = req.BrandName.Trim(),
            BrandCode = string.IsNullOrWhiteSpace(req.BrandCode) ? req.BrandName.Trim().ToUpper() : req.BrandCode.Trim().ToUpper(),
            Description = req.Description,
            Priority = req.Priority ?? 0,
            IsActive = true,
            IsDeleted = false
        };

        _context.Brands.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success("Brand created successfully.", entity.ID));
    }

    [HttpPut("brands/{id}")]
    public async Task<IActionResult> UpdateBrand(long id, [FromBody] BrandRequest req)
    {
        var entity = await _context.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.ID == id && !b.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Brand not found.", 404));

        if (!string.IsNullOrWhiteSpace(req.BrandName)) entity.BrandName = req.BrandName.Trim();
        if (req.BrandCode != null) entity.BrandCode = req.BrandCode.Trim().ToUpper();
        if (req.Description != null) entity.Description = req.Description;
        if (req.Priority.HasValue) entity.Priority = req.Priority.Value;

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Brand updated successfully.", entity.ID));
    }

    [HttpPut("brands/{id}/toggle-active")]
    public async Task<IActionResult> ToggleBrandActive(long id)
    {
        var entity = await _context.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.ID == id && !b.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Brand not found.", 404));

        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success($"Brand is now {(entity.IsActive ? "Active" : "Inactive")}.", entity.ID));
    }

    [HttpDelete("brands/{id}")]
    public async Task<IActionResult> DeleteBrand(long id)
    {
        var entity = await _context.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.ID == id && !b.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Brand not found.", 404));

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Brand deleted successfully.", entity.ID));
    }

    // =========================================
    // 5. UNIFIED DROPDOWNS API
    // =========================================

    [HttpGet("all-dropdowns")]
    public async Task<IActionResult> GetAllDropdowns([FromQuery] long? tenantId, [FromQuery] string? tenantCode)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);

        var categories = await _context.ItemCategories.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.TenantId == tId && c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Priority).ThenBy(c => c.CategoryName)
            .Select(c => new { id = c.ID, name = c.CategoryName, code = c.CategoryCode })
            .ToListAsync();

        var uoms = await _context.UnitOfMeasurements.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.TenantId == tId && u.IsActive && !u.IsDeleted)
            .OrderBy(u => u.Priority).ThenBy(u => u.UOMName)
            .Select(u => new { id = u.ID, name = u.UOMName, code = u.UOMCode, symbol = u.Symbol, precision = u.DecimalPrecision })
            .ToListAsync();

        var itemTypes = await _context.ItemTypes.IgnoreQueryFilters().AsNoTracking()
            .Where(t => t.TenantId == tId && t.IsActive && !t.IsDeleted)
            .OrderBy(t => t.Priority).ThenBy(t => t.ItemTypeName)
            .Select(t => new { id = t.ID, name = t.ItemTypeName, code = t.ItemTypeCode })
            .ToListAsync();

        var brands = await _context.Brands.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.TenantId == tId && b.IsActive && !b.IsDeleted)
            .OrderBy(b => b.Priority).ThenBy(b => b.BrandName)
            .Select(b => new { id = b.ID, name = b.BrandName, code = b.BrandCode })
            .ToListAsync();

        return Ok(new
        {
            categories = categories,
            uoms = uoms,
            itemTypes = itemTypes,
            brands = brands
        });
    }
}

public class CategoryRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryCode { get; set; }
    public string? Description { get; set; }
    public int? Priority { get; set; }
}

public class UomRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string UOMName { get; set; } = string.Empty;
    public string? UOMCode { get; set; }
    public string? Symbol { get; set; }
    public int? DecimalPrecision { get; set; }
    public string? Description { get; set; }
    public int? Priority { get; set; }
}

public class ItemTypeRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string ItemTypeName { get; set; } = string.Empty;
    public string? ItemTypeCode { get; set; }
    public string? Description { get; set; }
    public int? Priority { get; set; }
}

public class BrandRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? BrandCode { get; set; }
    public string? Description { get; set; }
    public int? Priority { get; set; }
}
