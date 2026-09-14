using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Customer : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Mobile { get; set; }

    public string? Address { get; set; }

    public string? Village { get; set; }

    public decimal CreditLimit { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal CurrentBalance { get; set; }
}
