using System;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class AuditLog : BaseTenantEntity
{
    public long UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public long EntityId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? DeviceId { get; set; }
}
