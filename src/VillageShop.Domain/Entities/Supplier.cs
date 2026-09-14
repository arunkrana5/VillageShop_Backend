using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Supplier : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Mobile { get; set; }

    public string? Address { get; set; }

    public string? GSTNumber { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal CurrentBalance { get; set; }
}
