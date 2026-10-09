using System.ComponentModel.DataAnnotations.Schema;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

[Table("V_Masters")]
public class Master : BaseTenantEntity
{
    public string MasterType { get; set; } = string.Empty;

    public string MasterName { get; set; } = string.Empty;

    public string MasterCode { get; set; } = string.Empty;

    public string? Description { get; set; }

    public long? ParentId { get; set; }

    public int SortOrder { get; set; } = 0;
}
