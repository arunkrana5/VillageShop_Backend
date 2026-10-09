using System.ComponentModel.DataAnnotations.Schema;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

[Table("V_UnitOfMeasurements")]
public class UnitOfMeasurement : BaseTenantEntity
{
    public string UOMName { get; set; } = string.Empty;

    public string UOMCode { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public int DecimalPrecision { get; set; } = 0;

    public string? Description { get; set; }
}
