namespace VillageShop.Application.Products.DTOs;

public class CreateProductRequest
{
    public long? TenantId { get; set; }

    public string? TenantCode { get; set; }

    public string? ProductCode { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Brand { get; set; }

    public string? Unit { get; set; }

    public string? Barcode { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal MRP { get; set; }

    public decimal GSTPercent { get; set; }

    public decimal OpeningStock { get; set; }

    public decimal? CurrentStock { get; set; }

    public decimal MinimumStock { get; set; }

    public string? BatchNumber { get; set; }

    public string? RackNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? HSNCode { get; set; }
}

public class UpdateProductRequest : CreateProductRequest
{
    public long ID { get; set; }
}

public class ProductSearchRequest
{
    public long? TenantId { get; set; }

    public string? TenantCode { get; set; }

    public string Search { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 200;

    public string SortBy { get; set; } = "ID";

    public string SortDirection { get; set; } = "DESC";
}
