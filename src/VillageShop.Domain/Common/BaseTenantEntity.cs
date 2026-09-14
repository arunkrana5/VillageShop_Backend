namespace VillageShop.Domain.Common;

public abstract class BaseTenantEntity : BaseEntity
{
    public long TenantId { get; set; }
}
