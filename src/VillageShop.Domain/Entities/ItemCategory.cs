using System.ComponentModel.DataAnnotations.Schema;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

[Table("V_ItemCategories")]
public class ItemCategory : BaseTenantEntity
{
    public string CategoryName { get; set; } = string.Empty;

    public string? CategoryCode { get; set; }

    public string? Description { get; set; }
}
