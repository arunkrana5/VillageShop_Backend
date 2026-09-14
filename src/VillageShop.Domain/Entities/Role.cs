using System.Collections.Generic;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Role : BaseTenantEntity
{
    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; } = false;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
