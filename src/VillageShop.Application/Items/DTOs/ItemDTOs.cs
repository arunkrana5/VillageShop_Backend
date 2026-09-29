namespace VillageShop.Application.Items.DTOs;

public class CreateItemRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Unit { get; set; } = "pcs";
    public string Format { get; set; } = "Packed";
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
    public string Unit { get; set; } = "pcs";
    public string Format { get; set; } = "Packed";
    public string? Description { get; set; }
}

public class ItemSearchRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 200;
}
