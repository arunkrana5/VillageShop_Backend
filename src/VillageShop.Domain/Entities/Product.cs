using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Product : BaseTenantEntity
{
    public string ProductCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Brand { get; set; }

    public string Unit { get; set; } = "pcs";

    public string? Barcode { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal MRP { get; set; }

    public decimal GSTPercent { get; set; }

    public decimal OpeningStock { get; set; }

    public decimal MinimumStock { get; set; }

    public decimal CurrentStock { get; set; }

    public string? BatchNumber { get; set; }

    public string? RackNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? HSNCode { get; set; }
}
