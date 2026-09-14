using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class TenantConfiguration : BaseTenantEntity
{
    public string BrandingJson { get; set; } = "{}";

    public string FeatureJson { get; set; } = "{}";

    public string MenuJson { get; set; } = "[]";

    public string CustomFieldsJson { get; set; } = "[]";
}
