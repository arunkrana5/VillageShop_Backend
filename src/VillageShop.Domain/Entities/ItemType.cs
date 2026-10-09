using System.ComponentModel.DataAnnotations.Schema;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

[Table("V_ItemTypes")]
public class ItemType : BaseTenantEntity
{
    public string ItemTypeName { get; set; } = string.Empty;

    public string? ItemTypeCode { get; set; }

    public string? Description { get; set; }
}
