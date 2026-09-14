using System;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class User : BaseTenantEntity
{
    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Mobile { get; set; }

    public long RoleId { get; set; }

    public Role? Role { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }
}
