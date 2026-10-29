using System;
using System.Collections.Generic;

namespace VillageShop.Application.Uploader.DTOs;

public class UploadTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string GroupType { get; set; } = "Master"; // "Transaction" or "Master"
    public string Name { get; set; } = string.Empty;
    public string StageTable { get; set; } = string.Empty;
    public string MainTable { get; set; } = string.Empty;
    public List<UploadColumnDto> Columns { get; set; } = new();
}

public class UploadColumnDto
{
    public int Id { get; set; }
    public int UploadTypeId { get; set; }
    public string ExcelHeader { get; set; } = string.Empty;
    public string DbColumn { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int? MaxLength { get; set; }
    public bool IsKey { get; set; }
    public string? SampleValue { get; set; }
    public int SortOrder { get; set; }
}

public class UploadBatchDto
{
    public Guid BatchId { get; set; }
    public long TenantId { get; set; }
    public int UploadTypeId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string StageTable { get; set; } = string.Empty;
    public string MainTable { get; set; } = string.Empty;
    public string? SyncProcName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
}

public class BatchRowResultDto
{
    public int RowNo { get; set; }
    public string KeyValue { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public Dictionary<string, object?> Values { get; set; } = new();
}
