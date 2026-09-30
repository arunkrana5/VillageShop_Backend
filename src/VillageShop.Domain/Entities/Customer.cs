using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Customer : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Mobile { get; set; }

    public string? Address { get; set; }

    public string? Village { get; set; }

    public string? Email { get; set; }

    public string? WhatsApp { get; set; }

    public string? FatherName { get; set; }

    public string? PO { get; set; }

    public string? PS { get; set; }

    public string? Dist { get; set; }

    public string? Pincode { get; set; }

    public decimal CreditLimit { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal CurrentBalance { get; set; }
}
