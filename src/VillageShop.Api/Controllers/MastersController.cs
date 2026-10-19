using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        if (targetTenantId <= 0 && User?.Claims != null)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "TenantId" || c.Type == "tenant_id" || c.Type == "tenantId");
            if (claim != null && long.TryParse(claim.Value, out var cTid) && cTid > 0)
            {
                targetTenantId = cTid;
            }
        }

        if (targetTenantId <= 0) targetTenantId = 1;
        return targetTenantId;
    }

    // =========================================
    // 1. UNIFIED GENERIC MASTER ENDPOINTS
    // =========================================

    [HttpGet]
    public async Task<IActionResult> GetMasters([FromQuery] string? masterType, [FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 0)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);
        var baseQuery = _context.Masters.IgnoreQueryFilters().AsNoTracking().Where(m => m.TenantId == tId && !m.IsDeleted);

        // If tenant has no custom masters, fallback to global default masters from Tenant 1
        if (tId != 1 && !await baseQuery.AnyAsync())
        {
            baseQuery = _context.Masters.IgnoreQueryFilters().AsNoTracking().Where(m => m.TenantId == 1 && !m.IsDeleted);
        }

        var query = baseQuery;
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

        int totalCount = await query.CountAsync();
        var orderedQuery = query.OrderBy(m => m.MasterType).ThenBy(m => m.Priority).ThenBy(m => m.MasterName).ThenBy(m => m.ID);

        if (pageSize > 0)
        {
            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page-Number"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
            bool hasMore = page * pageSize < totalCount;
            Response.Headers["X-Has-More"] = hasMore.ToString().ToLower();
            orderedQuery = (IOrderedQueryable<Master>)orderedQuery.Skip((page - 1) * pageSize).Take(pageSize);
        }

        var list = await orderedQuery
            .Select(m => new
            {
                id = m.ID,
                tenantId = m.TenantId,
                masterType = m.MasterType,
                masterName = string.IsNullOrWhiteSpace(m.MasterName) ? "Master Item #" + m.ID : m.MasterName,
                name = string.IsNullOrWhiteSpace(m.MasterName) ? "Master Item #" + m.ID : m.MasterName,
                masterCode = m.MasterCode ?? "",
                code = m.MasterCode ?? "",
                description = m.Description ?? "",
                priority = m.Priority,
                isActive = m.IsActive,
                isDeleted = m.IsDeleted,
                createdBy = m.CreatedBy,
                createdDate = m.CreatedDate.ToString("o"),
                modifiedBy = m.ModifiedBy,
                modifiedDate = m.ModifiedDate.ToString("o"),
                entrySource = m.EntrySource ?? "API",
                ipAddress = m.IPAddress ?? "127.0.0.1"
            })
            .ToListAsync();

        bool isPaged = (Request.Query.ContainsKey("paged") && Request.Query["paged"] == "true") || (Request.Query.ContainsKey("page") && pageSize > 0 && Request.Query.ContainsKey("pageSize"));
        if (isPaged && pageSize > 0)
        {
            return Ok(PagedResult<object>.Create(list.Cast<object>().ToList(), totalCount, page, pageSize));
        }

        return Ok(list);
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetMasterTypes([FromQuery] long? tenantId, [FromQuery] string? tenantCode)
    {
        long tId = ResolveTenantId(tenantId, tenantCode);
        var baseQuery = _context.Masters.IgnoreQueryFilters().AsNoTracking().Where(m => m.TenantId == tId && !m.IsDeleted && !string.IsNullOrWhiteSpace(m.MasterType));
        if (tId != 1 && !await baseQuery.AnyAsync())
        {
            baseQuery = _context.Masters.IgnoreQueryFilters().AsNoTracking().Where(m => m.TenantId == 1 && !m.IsDeleted && !string.IsNullOrWhiteSpace(m.MasterType));
        }

        var types = await baseQuery
            .Select(m => m.MasterType.Trim())
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMaster([FromBody] GenericMasterRequest req)
    {
        var effectiveName = req?.GetEffectiveMasterName();
        if (req == null || string.IsNullOrWhiteSpace(effectiveName))
            return BadRequest(PostResponse.Error("Master Name is required."));

        string masterType = req.GetEffectiveMasterType();
        long tId = ResolveTenantId(req.TenantId, req.TenantCode);

        bool exists = await _context.Masters.IgnoreQueryFilters().AnyAsync(m => m.TenantId == tId 
            && m.MasterType.ToLower() == masterType.ToLower() 
            && m.MasterName.ToLower() == effectiveName.ToLower() 
            && !m.IsDeleted);

        if (exists) return BadRequest(PostResponse.Error($"Master record '{effectiveName}' already exists under type '{masterType}'."));

        var effectiveCode = req.GetEffectiveMasterCode();
        var entity = new Master
        {
            TenantId = tId,
            MasterType = masterType,
            MasterName = effectiveName,
            MasterCode = string.IsNullOrWhiteSpace(effectiveCode) ? $"{masterType.Substring(0, Math.Min(3, masterType.Length)).ToUpper()}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}" : effectiveCode,
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

        var mType = req?.GetEffectiveMasterType();
        if (!string.IsNullOrWhiteSpace(mType) && mType != "General") entity.MasterType = mType;

        var mName = req?.GetEffectiveMasterName();
        if (!string.IsNullOrWhiteSpace(mName)) entity.MasterName = mName;

        var mCode = req?.GetEffectiveMasterCode();
        if (mCode != null) entity.MasterCode = mCode;

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

        var baseQuery = _context.Masters.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.TenantId == tId && m.IsActive && !m.IsDeleted);

        if (tId != 1 && !await baseQuery.AnyAsync())
        {
            baseQuery = _context.Masters.IgnoreQueryFilters().AsNoTracking()
                .Where(m => m.TenantId == 1 && m.IsActive && !m.IsDeleted);
        }

        var masters = await baseQuery
            .OrderBy(m => m.Priority).ThenBy(m => m.MasterName)
            .Select(m => new { id = m.ID, masterType = m.MasterType, masterName = m.MasterName, masterCode = m.MasterCode, name = m.MasterName, code = m.MasterCode })
            .ToListAsync();

        var categories = masters.Where(m => m.masterType.Equals("ItemCategory", StringComparison.OrdinalIgnoreCase)).ToList();
        var uoms = masters.Where(m => m.masterType.Equals("UnitOfMeasurement", StringComparison.OrdinalIgnoreCase)).ToList();
        var itemTypes = masters.Where(m => m.masterType.Equals("ItemType", StringComparison.OrdinalIgnoreCase)).ToList();
        var brands = masters.Where(m => m.masterType.Equals("Brand", StringComparison.OrdinalIgnoreCase)).ToList();
        var paymentModes = masters.Where(m => m.masterType.Equals("PaymentMode", StringComparison.OrdinalIgnoreCase)).ToList();
        var taxes = masters.Where(m => m.masterType.Equals("Tax", StringComparison.OrdinalIgnoreCase)).ToList();
        var expenseCategories = masters.Where(m => m.masterType.Equals("ExpenseCategory", StringComparison.OrdinalIgnoreCase)).ToList();

        return Ok(new
        {
            categories = categories,
            uoms = uoms,
            itemTypes = itemTypes,
            brands = brands,
            paymentModes = paymentModes,
            taxes = taxes,
            expenseCategories = expenseCategories,
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

    [HttpGet("payment-modes")]
    public Task<IActionResult> GetPaymentModes([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
        => GetMasters("PaymentMode", tenantId, tenantCode, search, activeOnly);

    [HttpPost("payment-modes")]
    public Task<IActionResult> CreatePaymentMode([FromBody] GenericMasterRequest req)
    {
        req.MasterType = "PaymentMode";
        return CreateMaster(req);
    }

    [HttpPut("payment-modes/{id}")]
    public Task<IActionResult> UpdatePaymentMode(long id, [FromBody] GenericMasterRequest req)
    {
        req.MasterType = "PaymentMode";
        return UpdateMaster(id, req);
    }

    [HttpGet("taxes")]
    public Task<IActionResult> GetTaxes([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
        => GetMasters("Tax", tenantId, tenantCode, search, activeOnly);

    [HttpPost("taxes")]
    public Task<IActionResult> CreateTax([FromBody] GenericMasterRequest req)
    {
        req.MasterType = "Tax";
        return CreateMaster(req);
    }

    [HttpPut("taxes/{id}")]
    public Task<IActionResult> UpdateTax(long id, [FromBody] GenericMasterRequest req)
    {
        req.MasterType = "Tax";
        return UpdateMaster(id, req);
    }

    [HttpGet("expense-categories")]
    public Task<IActionResult> GetExpenseCategories([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? search, [FromQuery] bool activeOnly = false)
        => GetMasters("ExpenseCategory", tenantId, tenantCode, search, activeOnly);

    [HttpPost("expense-categories")]
    public Task<IActionResult> CreateExpenseCategory([FromBody] GenericMasterRequest req)
    {
        req.MasterType = "ExpenseCategory";
        return CreateMaster(req);
    }

    [HttpPut("expense-categories/{id}")]
    public Task<IActionResult> UpdateExpenseCategory(long id, [FromBody] GenericMasterRequest req)
    {
        req.MasterType = "ExpenseCategory";
        return UpdateMaster(id, req);
    }
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

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string GetEffectiveMasterType()
    {
        if (!string.IsNullOrWhiteSpace(MasterType)) return MasterType.Trim();
        if (ExtensionData != null)
        {
            foreach (var kvp in ExtensionData)
            {
                var k = kvp.Key.ToLower();
                if (k == "mastertype" || k == "type")
                {
                    if (kvp.Value.ValueKind == JsonValueKind.String)
                    {
                        var s = kvp.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                    }
                }
            }
        }
        return "General";
    }

    public string? GetEffectiveMasterName()
    {
        if (!string.IsNullOrWhiteSpace(MasterName)) return MasterName.Trim();
        if (ExtensionData != null)
        {
            foreach (var kvp in ExtensionData)
            {
                var k = kvp.Key.ToLower();
                if (k == "mastername" || k == "name" || k == "categoryname" || k == "uomname" || 
                    k == "itemtypename" || k == "brandname" || k == "paymentmodename" || 
                    k == "taxname" || k == "expensecategoryname" || k == "expensename" || 
                    k == "title" || k == "label")
                {
                    if (kvp.Value.ValueKind == JsonValueKind.String)
                    {
                        var s = kvp.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                    }
                }
            }
        }
        return null;
    }

    public string? GetEffectiveMasterCode()
    {
        if (!string.IsNullOrWhiteSpace(MasterCode)) return MasterCode.Trim();
        if (ExtensionData != null)
        {
            foreach (var kvp in ExtensionData)
            {
                var k = kvp.Key.ToLower();
                if (k == "mastercode" || k == "code" || k == "symbol" || 
                    k == "categorycode" || k == "uomcode" || k == "itemtypecode" || 
                    k == "brandcode" || k == "shortname")
                {
                    if (kvp.Value.ValueKind == JsonValueKind.String)
                    {
                        var s = kvp.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                    }
                }
            }
        }
        return null;
    }
}
