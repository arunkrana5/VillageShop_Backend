using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Item : BaseTenantEntity
{
    public string ItemCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string Unit { get; set; } = "pcs";

    public string Format { get; set; } = "Packed";

    public string? Description { get; set; }
}
