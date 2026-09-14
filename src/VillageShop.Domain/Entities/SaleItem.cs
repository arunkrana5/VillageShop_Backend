using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class SaleItem : BaseTenantEntity
{
    public long SaleId { get; set; }

    public Sale Sale { get; set; } = null!;

    public long ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TaxPercent { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }
}
