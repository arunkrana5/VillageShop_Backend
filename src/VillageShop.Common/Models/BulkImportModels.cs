using System.Collections.Generic;

namespace VillageShop.Common.Models;

public class BulkImportItemRow
{
    public int RowIndex { get; set; }
    public string? ItemCode { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Unit { get; set; } = "pcs";
    public string? Format { get; set; } = "Packed";
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? MRP { get; set; }
    public decimal? GSTPercent { get; set; }
    public decimal? OpeningStock { get; set; }
    public decimal? CurrentStock { get; set; }
    public decimal? MinimumStock { get; set; } = 5;
    public string? Barcode { get; set; }
}

public class BulkImportRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public List<BulkImportItemRow> Rows { get; set; } = new();
    public List<BulkImportItemRow> Items
    {
        get => Rows;
        set => Rows = value;
    }
}


public class BulkImportRowError
{
    public int RowIndex { get; set; }
    public string Field { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? RawData { get; set; }
}

public class BulkImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkImportRowError> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
