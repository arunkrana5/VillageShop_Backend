using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Permission : BaseEntity
{
    public string PermissionKey { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string? Description { get; set; }
}
