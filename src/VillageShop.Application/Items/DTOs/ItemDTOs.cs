using VillageShop.Common.Models;

namespace VillageShop.Application.Items.DTOs;

public class CreateItemRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public long? CategoryId { get; set; }
    public string Unit { get; set; } = "pcs";
    public long? UnitId { get; set; }
    public string Format { get; set; } = "Packed";
    public long? ItemTypeId { get; set; }
    public long? BrandId { get; set; }
    public string? Description { get; set; }
}

public class UpdateItemRequest
{
    public long ID { get; set; }
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public long? CategoryId { get; set; }
    public string Unit { get; set; } = "pcs";
    public long? UnitId { get; set; }
    public string Format { get; set; } = "Packed";
    public long? ItemTypeId { get; set; }
    public long? BrandId { get; set; }
    public string? Description { get; set; }
}

public class ItemSearchRequest : PagedRequest
{
    public long? CategoryId { get; set; }
    public string? Category { get; set; }
    public bool Paged { get; set; } = false;
}

