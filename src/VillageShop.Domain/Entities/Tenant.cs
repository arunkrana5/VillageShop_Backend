using System;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Tenant : BaseEntity
{
    public string TenantCode { get; set; } = string.Empty;

    public string TenantName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? Village { get; set; }

    public string? District { get; set; }

    public string? State { get; set; }

    public string? Pincode { get; set; }

    public string? LogoUrl { get; set; }
}
