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
    // 1. UNIFIED GENERIC MASTER ENDPOINTS
    // =========================================

    [HttpGet]
    public async Task<IActionResult> GetMasters([FromQuery] string? masterType, [FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);
        var query = _context.Masters.IgnoreQueryFilters().AsNoTracking().Where(m => m.TenantId == tId && !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(masterType))
        {
            var mType = masterType.Trim().ToLower();
            query = query.Where(m => m.MasterType.ToLower() == mType);
        }

        if (activeOnly) query = query.Where(m => m.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(m => m.MasterName.ToLower().Contains(s) || (m.MasterCode != null && m.MasterCode.ToLower().Contains(s)) || (m.Description != null && m.Description.ToLower().Contains(s)));
        }

        var list = await query.OrderBy(m => m.MasterType).ThenBy(m => m.Priority).ThenBy(m => m.MasterName)
            .Select(m => new
            {
                id = m.ID,
                ID = m.ID,
                tenantId = m.TenantId,
                TenantId = m.TenantId,
                masterType = m.MasterType,
                MasterType = m.MasterType,
                masterName = string.IsNullOrWhiteSpace(m.MasterName) ? "Master Item #" + m.ID : m.MasterName,
                MasterName = string.IsNullOrWhiteSpace(m.MasterName) ? "Master Item #" + m.ID : m.MasterName,
                name = string.IsNullOrWhiteSpace(m.MasterName) ? "Master Item #" + m.ID : m.MasterName,
                Name = string.IsNullOrWhiteSpace(m.MasterName) ? "Master Item #" + m.ID : m.MasterName,
                masterCode = m.MasterCode ?? "",
                MasterCode = m.MasterCode ?? "",
                code = m.MasterCode ?? "",
                Code = m.MasterCode ?? "",
                description = m.Description ?? "",
                Description = m.Description ?? "",
                priority = m.Priority,
                Priority = m.Priority,
                isActive = m.IsActive,
                IsActive = m.IsActive,
                isDeleted = m.IsDeleted,
                IsDeleted = m.IsDeleted,
                createdBy = m.CreatedBy,
                CreatedBy = m.CreatedBy,
                createdDate = m.CreatedDate.ToString("o"),
                CreatedDate = m.CreatedDate.ToString("o"),
                modifiedBy = m.ModifiedBy,
                ModifiedBy = m.ModifiedBy,
                modifiedDate = m.ModifiedDate.ToString("o"),
                ModifiedDate = m.ModifiedDate.ToString("o"),
                entrySource = m.EntrySource ?? "API",
                EntrySource = m.EntrySource ?? "API",
                ipAddress = m.IPAddress ?? "127.0.0.1",
                IPAddress = m.IPAddress ?? "127.0.0.1"
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetMasterTypes([FromQuery] long? tenantId, [FromQuery] string? tenantCode)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);
        var types = await _context.Masters.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.TenantId == tId && !m.IsDeleted)
            .Select(m => m.MasterType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMaster([FromBody] GenericMasterRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.MasterName))
            return BadRequest(PostResponse.Error("Master Name is required."));

        string masterType = string.IsNullOrWhiteSpace(req.MasterType) ? "General" : req.MasterType.Trim();
        long tId = ResolveTenantId(req.TenantId, req.TenantCode);

        bool exists = await _context.Masters.IgnoreQueryFilters().AnyAsync(m => m.TenantId == tId 
            && m.MasterType.ToLower() == masterType.ToLower() 
            && m.MasterName.ToLower() == req.MasterName.Trim().ToLower() 
            && !m.IsDeleted);

        if (exists) return BadRequest(PostResponse.Error($"Master record '{req.MasterName}' already exists under type '{masterType}'."));

        var entity = new Master
        {
            TenantId = tId,
            MasterType = masterType,
            MasterName = req.MasterName.Trim(),
            MasterCode = string.IsNullOrWhiteSpace(req.MasterCode) ? $"{masterType.Substring(0, Math.Min(3, masterType.Length)).ToUpper()}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}" : req.MasterCode.Trim(),
            Description = req.Description,
            Priority = req.Priority ?? 0,
            IsActive = true,
            IsDeleted = false
        };

        _context.Masters.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success("Master record created successfully.", entity.ID));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMaster(long id, [FromBody] GenericMasterRequest req)
    {
        var entity = await _context.Masters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ID == id && !m.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Master record not found.", 404));

        if (!string.IsNullOrWhiteSpace(req?.MasterType)) entity.MasterType = req.MasterType.Trim();
        if (!string.IsNullOrWhiteSpace(req?.MasterName)) entity.MasterName = req.MasterName.Trim();
        if (req?.MasterCode != null) entity.MasterCode = req.MasterCode.Trim();
        if (req?.Description != null) entity.Description = req.Description;
        if (req?.Priority.HasValue == true) entity.Priority = req.Priority.Value;

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Master record updated successfully.", entity.ID));
    }

    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleMasterActive(long id)
    {
        var entity = await _context.Masters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ID == id && !m.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Master record not found.", 404));

        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success($"Master status changed to {(entity.IsActive ? "Active" : "Inactive")}.", entity.ID));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMaster(long id)
    {
        var entity = await _context.Masters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.ID == id && !m.IsDeleted);
        if (entity == null) return NotFound(PostResponse.Error("Master record not found.", 404));

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success("Master record deleted successfully.", entity.ID));
    }

    // =========================================
    // 2. UNIFIED DROPDOWNS API
    // =========================================

    [HttpGet("all-dropdowns")]
    public async Task<IActionResult> GetAllDropdowns([FromQuery] long? tenantId, [FromQuery] string? tenantCode)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);

        var masters = await _context.Masters.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.TenantId == tId && m.IsActive && !m.IsDeleted)
            .OrderBy(m => m.Priority).ThenBy(m => m.MasterName)
            .Select(m => new { id = m.ID, masterType = m.MasterType, masterName = m.MasterName, masterCode = m.MasterCode, name = m.MasterName, code = m.MasterCode })
            .ToListAsync();

        var categories = masters.Where(m => m.masterType.Equals("ItemCategory", StringComparison.OrdinalIgnoreCase)).ToList();
        var uoms = masters.Where(m => m.masterType.Equals("UnitOfMeasurement", StringComparison.OrdinalIgnoreCase)).ToList();
        var itemTypes = masters.Where(m => m.masterType.Equals("ItemType", StringComparison.OrdinalIgnoreCase)).ToList();
        var brands = masters.Where(m => m.masterType.Equals("Brand", StringComparison.OrdinalIgnoreCase)).ToList();

        return Ok(new
        {
            categories = categories,
            uoms = uoms,
            itemTypes = itemTypes,
            brands = brands,
            allMasters = masters
        });
    }

    // =========================================
    // 3. BACKWARDS COMPATIBILITY ROUTE ALIASES
    // =========================================

    [HttpGet("categories")]
    public Task<IActionResult> GetCategories([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
        => GetMasters("ItemCategory", tenantId, tenantCode, search, activeOnly);

    [HttpPost("categories")]
    public Task<IActionResult> CreateCategory([FromBody] GenericMasterRequest req)
    {
        req.MasterType = "ItemCategory";
        return CreateMaster(req);
    }

    [HttpPut("categories/{id}")]
    public Task<IActionResult> UpdateCategory(long id, [FromBody] GenericMasterRequest req)
    {
        req.MasterType = "ItemCategory";
        return UpdateMaster(id, req);
    }

    [HttpPut("categories/{id}/toggle-active")]
    public Task<IActionResult> ToggleCategoryActive(long id) => ToggleMasterActive(id);

    [HttpDelete("categories/{id}")]
    public Task<IActionResult> DeleteCategory(long id) => DeleteMaster(id);

    [HttpGet("uoms")]
    public Task<IActionResult> GetUOMs([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
        => GetMasters("UnitOfMeasurement", tenantId, tenantCode, search, activeOnly);

    [HttpPost("uoms")]
    public Task<IActionResult> CreateUOM([FromBody] GenericMasterRequest req)
    {
        req.MasterType = "UnitOfMeasurement";
        return CreateMaster(req);
    }

    [HttpPut("uoms/{id}")]
    public Task<IActionResult> UpdateUOM(long id, [FromBody] GenericMasterRequest req)
    {
        req.MasterType = "UnitOfMeasurement";
        return UpdateMaster(id, req);
    }

    [HttpPut("uoms/{id}/toggle-active")]
    public Task<IActionResult> ToggleUomActive(long id) => ToggleMasterActive(id);

    [HttpDelete("uoms/{id}")]
    public Task<IActionResult> DeleteUom(long id) => DeleteMaster(id);

    [HttpGet("item-types")]
    public Task<IActionResult> GetItemTypes([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
        => GetMasters("ItemType", tenantId, tenantCode, search, activeOnly);

    [HttpPost("item-types")]
    public Task<IActionResult> CreateItemType([FromBody] GenericMasterRequest req)
    {
        req.MasterType = "ItemType";
        return CreateMaster(req);
    }

    [HttpPut("item-types/{id}")]
    public Task<IActionResult> UpdateItemType(long id, [FromBody] GenericMasterRequest req)
    {
        req.MasterType = "ItemType";
        return UpdateMaster(id, req);
    }

    [HttpPut("item-types/{id}/toggle-active")]
    public Task<IActionResult> ToggleItemTypeActive(long id) => ToggleMasterActive(id);

    [HttpDelete("item-types/{id}")]
    public Task<IActionResult> DeleteItemType(long id) => DeleteMaster(id);

    [HttpGet("brands")]
    public Task<IActionResult> GetBrands([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
        => GetMasters("Brand", tenantId, tenantCode, search, activeOnly);

    [HttpPost("brands")]
    public Task<IActionResult> CreateBrand([FromBody] GenericMasterRequest req)
    {
        req.MasterType = "Brand";
        return CreateMaster(req);
    }

    [HttpPut("brands/{id}")]
    public Task<IActionResult> UpdateBrand(long id, [FromBody] GenericMasterRequest req)
    {
        req.MasterType = "Brand";
        return UpdateMaster(id, req);
    }

    [HttpPut("brands/{id}/toggle-active")]
    public Task<IActionResult> ToggleBrandActive(long id) => ToggleMasterActive(id);

    [HttpDelete("brands/{id}")]
    public Task<IActionResult> DeleteBrand(long id) => DeleteMaster(id);
}

public class GenericMasterRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string? MasterType { get; set; }
    public string? MasterName { get; set; }
    public string? MasterCode { get; set; }
    public string? Description { get; set; }
    public int? Priority { get; set; }
}
