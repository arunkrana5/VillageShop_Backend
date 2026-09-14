using System;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class SyncQueue : BaseTenantEntity
{
    public string ClientTransactionId { get; set; } = string.Empty;

    public string? DeviceId { get; set; }

    public long UserId { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string SyncStatus { get; set; } = "PENDING";

    public int RetryCount { get; set; } = 0;

    public string? ErrorMessage { get; set; }
}
