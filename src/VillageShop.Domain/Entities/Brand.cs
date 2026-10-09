using System.ComponentModel.DataAnnotations.Schema;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

[Table("V_Brands")]
public class Brand : BaseTenantEntity
{
    public string BrandName { get; set; } = string.Empty;

    public string? BrandCode { get; set; }

    public string? Description { get; set; }
}
