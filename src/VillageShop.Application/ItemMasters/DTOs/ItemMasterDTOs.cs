namespace VillageShop.Application.ItemMasters.DTOs;

public class CreateItemMasterRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Unit { get; set; } = "pcs";
    public string Format { get; set; } = "Packed";
    public string? Description { get; set; }
}

public class UpdateItemMasterRequest
{
    public long ID { get; set; }
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Unit { get; set; } = "pcs";
    public string Format { get; set; } = "Packed";
    public string? Description { get; set; }
}

public class ItemMasterSearchRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 200;
}
