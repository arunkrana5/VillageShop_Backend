using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using VillageShop.Application.Uploader.DTOs;

namespace VillageShop.Infrastructure.Uploader;

public interface IExcelUploaderService
{
    Task<IEnumerable<UploadTypeDto>> GetActiveTypesAsync();
    Task<byte[]> GenerateTemplateAsync(string typeCode);
    Task<Guid> StageExcelAsync(string typeCode, Stream fileStream, string fileName, long tenantId, long userId, CancellationToken ct);
    Task<bool> QueueProcessingAsync(Guid batchId, long tenantId, long userId);
    Task<UploadBatchDto?> GetBatchStatusAsync(Guid batchId);
    Task<IEnumerable<UploadBatchDto>> GetBatchesPagedAsync(long tenantId, int page, int pageSize);
    Task<(int TotalCount, IEnumerable<BatchRowResultDto> Rows)> GetBatchRowsAsync(Guid batchId, string? status, int page, int pageSize);
    Task<byte[]> GenerateBatchReportAsync(Guid batchId);
    Task ProcessBatchAsync(Guid batchId, long tenantId, long userId, CancellationToken ct);
}

public class ExcelUploaderService : IExcelUploaderService
{
    private readonly string _connectionString;
    private readonly UploadProcessingChannel _channel;
    private readonly ILogger<ExcelUploaderService> _logger;

    public ExcelUploaderService(Microsoft.Extensions.Configuration.IConfiguration config, UploadProcessingChannel channel, ILogger<ExcelUploaderService> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        _channel = channel;
        _logger = logger;
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<IEnumerable<UploadTypeDto>> GetActiveTypesAsync()
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT Id, Code, Name, StageTable, MainTable FROM dbo.UploadType WHERE IsActive = 1 ORDER BY SortOrder;
            SELECT Id, UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder
            FROM dbo.UploadColumn ORDER BY SortOrder;";

        using var multi = await conn.QueryMultipleAsync(sql);
        var types = (await multi.ReadAsync<UploadTypeDto>()).ToList();
        var columns = (await multi.ReadAsync<UploadColumnDto>()).ToList();

        foreach (var t in types)
        {
            t.Columns = columns.Where(c => c.UploadTypeId == t.Id).ToList();
        }
        return types;
    }

    public async Task<byte[]> GenerateTemplateAsync(string typeCode)
    {
        var types = await GetActiveTypesAsync();
        var type = types.FirstOrDefault(t => t.Code.Equals(typeCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Upload type '{typeCode}' not found.");

        using var memoryStream = new MemoryStream();
        var sampleRow = new Dictionary<string, object?>();

        foreach (var col in type.Columns.OrderBy(c => c.SortOrder))
        {
            sampleRow[col.ExcelHeader] = col.SampleValue ?? (col.IsRequired ? "Required" : "Optional");
        }

        var rows = new List<Dictionary<string, object?>> { sampleRow };
        await memoryStream.SaveAsAsync(rows, sheetName: type.Code);
        return memoryStream.ToArray();
    }

    public async Task<Guid> StageExcelAsync(string typeCode, Stream fileStream, string fileName, long tenantId, long userId, CancellationToken ct)
    {
        var types = await GetActiveTypesAsync();
        var type = types.FirstOrDefault(t => t.Code.Equals(typeCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Type '{typeCode}' is not active.");

        var batchId = Guid.NewGuid();
        var columns = type.Columns.OrderBy(c => c.SortOrder).ToList();

        using var dataTable = new DataTable();
        dataTable.Columns.Add("BatchId", typeof(Guid));
        dataTable.Columns.Add("RowNo", typeof(int));
        foreach (var col in columns)
        {
            dataTable.Columns.Add(col.DbColumn, typeof(string));
        }
        dataTable.Columns.Add("RowStatus", typeof(string));
        dataTable.Columns.Add("ErrorMessage", typeof(string));

        int rowNumber = 1;
        var excelRows = fileStream.Query(useHeaderRow: true);

        foreach (IDictionary<string, object> row in excelRows)
        {
            ct.ThrowIfCancellationRequested();
            var dataRow = dataTable.NewRow();
            dataRow["BatchId"] = batchId;
            dataRow["RowNo"] = rowNumber++;

            foreach (var col in columns)
            {
                var key = row.Keys.FirstOrDefault(k => k.Trim().Equals(col.ExcelHeader.Trim(), StringComparison.OrdinalIgnoreCase))
                       ?? row.Keys.FirstOrDefault(k => k.Replace("*", "").Trim().Equals(col.ExcelHeader.Replace("*", "").Trim(), StringComparison.OrdinalIgnoreCase));

                dataRow[col.DbColumn] = key != null && row[key] != null ? row[key].ToString()?.Trim() : DBNull.Value;
            }

            dataRow["RowStatus"] = "Uploaded";
            dataRow["ErrorMessage"] = DBNull.Value;
            dataTable.Rows.Add(dataRow);
        }

        int totalRows = dataTable.Rows.Count;

        using var conn = GetConnection();
        await conn.OpenAsync(ct);
        using var tx = conn.BeginTransaction();

        const string insertBatchSql = @"
            INSERT INTO dbo.UploadBatch (BatchId, TenantId, UploadTypeId, FileName, TotalRows, SuccessRows, ErrorRows, Status, UploadedBy, CreatedOn)
            VALUES (@BatchId, @TenantId, @UploadTypeId, @FileName, @TotalRows, 0, 0, 'Uploaded', @UploadedBy, SYSUTCDATETIME());";

        await conn.ExecuteAsync(insertBatchSql, new
        {
            BatchId = batchId,
            TenantId = tenantId,
            UploadTypeId = type.Id,
            FileName = fileName,
            TotalRows = totalRows,
            UploadedBy = userId
        }, tx);

        if (totalRows > 0)
        {
            using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints, tx)
            {
                DestinationTableName = $"dbo.{type.StageTable}",
                BatchSize = 10000,
                BulkCopyTimeout = 300
            };

            bulkCopy.ColumnMappings.Add("BatchId", "BatchId");
            bulkCopy.ColumnMappings.Add("RowNo", "RowNo");
            foreach (var col in columns)
            {
                bulkCopy.ColumnMappings.Add(col.DbColumn, col.DbColumn);
            }
            bulkCopy.ColumnMappings.Add("RowStatus", "RowStatus");
            bulkCopy.ColumnMappings.Add("ErrorMessage", "ErrorMessage");

            await bulkCopy.WriteToServerAsync(dataTable, ct);
        }

        tx.Commit();
        return batchId;
    }

    public async Task<bool> QueueProcessingAsync(Guid batchId, long tenantId, long userId)
    {
        using var conn = GetConnection();
        var status = await conn.ExecuteScalarAsync<string>(
            "SELECT Status FROM dbo.UploadBatch WHERE BatchId = @BatchId", new { BatchId = batchId });

        if (status != "Uploaded" && status != "Failed")
            return false;

        await _channel.QueueAsync(new UploadWorkItem(batchId, tenantId, userId));
        return true;
    }

    public async Task ProcessBatchAsync(Guid batchId, long tenantId, long userId, CancellationToken ct)
    {
        using var conn = GetConnection();
        await conn.OpenAsync(ct);

        var batch = await conn.QueryFirstOrDefaultAsync<UploadBatchDto>(@"
            SELECT b.BatchId, b.UploadTypeId, b.TenantId, b.Status, t.Code AS TypeCode, t.StageTable, t.MainTable, t.SyncProcName
            FROM dbo.UploadBatch b
            INNER JOIN dbo.UploadType t ON b.UploadTypeId = t.Id
            WHERE b.BatchId = @BatchId", new { BatchId = batchId });

        if (batch == null || batch.Status == "Validating" || batch.Status == "Syncing" || batch.Status == "Completed")
            return;

        try
        {
            await conn.ExecuteAsync("UPDATE dbo.UploadBatch SET Status = 'Validating' WHERE BatchId = @BatchId", new { BatchId = batchId });

            var columns = (await conn.QueryAsync<UploadColumnDto>(
                "SELECT * FROM dbo.UploadColumn WHERE UploadTypeId = @UploadTypeId ORDER BY SortOrder",
                new { batch.UploadTypeId })).ToList();

            var validationSql = BuildSetBasedValidationSql(batch.StageTable, batchId, tenantId, columns);
            await conn.ExecuteAsync(validationSql, new { BatchId = batchId, TenantId = tenantId }, commandTimeout: 300);

            await conn.ExecuteAsync("UPDATE dbo.UploadBatch SET Status = 'Syncing' WHERE BatchId = @BatchId", new { BatchId = batchId });

            if (!string.IsNullOrWhiteSpace(batch.SyncProcName))
            {
                await conn.ExecuteAsync(batch.SyncProcName, new
                {
                    BatchId = batchId,
                    TenantId = tenantId,
                    UserId = userId
                }, commandType: CommandType.StoredProcedure, commandTimeout: 600);
            }

            const string summarizeSql = @"
                DECLARE @Success INT, @Errors INT;
                SELECT 
                    @Success = COUNT(CASE WHEN RowStatus = 'Synced' THEN 1 END),
                    @Errors = COUNT(CASE WHEN RowStatus = 'Error' THEN 1 END)
                FROM dbo.{0} WHERE BatchId = @BatchId;

                UPDATE dbo.UploadBatch
                SET Status = 'Completed',
                    SuccessRows = @Success,
                    ErrorRows = @Errors,
                    CompletedOn = SYSUTCDATETIME()
                WHERE BatchId = @BatchId;";

            await conn.ExecuteAsync(string.Format(summarizeSql, batch.StageTable), new { BatchId = batchId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Upload Batch {BatchId}", batchId);
            await conn.ExecuteAsync(@"
                UPDATE dbo.UploadBatch 
                SET Status = 'Failed', ErrorMessage = @ErrorMessage, CompletedOn = SYSUTCDATETIME() 
                WHERE BatchId = @BatchId",
                new { BatchId = batchId, ErrorMessage = ex.Message });
        }
    }

    private string BuildSetBasedValidationSql(string stageTable, Guid batchId, long tenantId, List<UploadColumnDto> columns)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"UPDATE dbo.{stageTable} SET RowStatus = 'Valid', ErrorMessage = NULL WHERE BatchId = @BatchId;");

        foreach (var col in columns.Where(c => c.IsRequired))
        {
            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), '{col.ExcelHeader} is required.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND (s.{col.DbColumn} IS NULL OR LTRIM(RTRIM(s.{col.DbColumn})) = '');");
        }

        foreach (var col in columns.Where(c => c.MaxLength.HasValue && c.DataType.Equals("NVARCHAR", StringComparison.OrdinalIgnoreCase)))
        {
            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), '{col.ExcelHeader} exceeds maximum length of {col.MaxLength}.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND s.{col.DbColumn} IS NOT NULL AND LEN(s.{col.DbColumn}) > {col.MaxLength};");
        }

        foreach (var col in columns.Where(c => c.DataType.Equals("DECIMAL", StringComparison.OrdinalIgnoreCase) || c.DataType.Equals("INT", StringComparison.OrdinalIgnoreCase)))
        {
            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), '{col.ExcelHeader} must be a valid numeric amount.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND s.{col.DbColumn} IS NOT NULL AND LTRIM(RTRIM(s.{col.DbColumn})) <> '' AND TRY_CAST(s.{col.DbColumn} AS DECIMAL(18,4)) IS NULL;");
        }

        var keyCols = columns.Where(c => c.IsKey).ToList();
        if (keyCols.Any())
        {
            var keyJoin = string.Join(" AND ", keyCols.Select(k => $"LOWER(RTRIM(LTRIM(t.{k.DbColumn}))) = LOWER(RTRIM(LTRIM(d.{k.DbColumn})))"));
            var keyColsSelect = string.Join(", ", keyCols.Select(k => $"LOWER(RTRIM(LTRIM({k.DbColumn}))) AS {k.DbColumn}"));
            var keyGroup = string.Join(", ", keyCols.Select(k => $"LOWER(RTRIM(LTRIM({k.DbColumn})))"));

            sb.AppendLine($@"
                WITH DupKeys AS (
                    SELECT {keyColsSelect}
                    FROM dbo.{stageTable}
                    WHERE BatchId = @BatchId
                    GROUP BY {keyGroup}
                    HAVING COUNT(1) > 1
                )
                UPDATE t
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), 'Duplicate key detected in upload file.')
                FROM dbo.{stageTable} t
                INNER JOIN DupKeys d ON {keyJoin}
                WHERE t.BatchId = @BatchId;");
        }

        return sb.ToString();
    }

    public async Task<UploadBatchDto?> GetBatchStatusAsync(Guid batchId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT b.BatchId, b.TenantId, b.UploadTypeId, t.Code AS TypeCode, t.Name AS TypeName, b.FileName,
                   b.TotalRows, b.SuccessRows, b.ErrorRows, b.Status, b.ErrorMessage, b.CreatedOn, b.CompletedOn
            FROM dbo.UploadBatch b
            INNER JOIN dbo.UploadType t ON b.UploadTypeId = t.Id
            WHERE b.BatchId = @BatchId";
        return await conn.QueryFirstOrDefaultAsync<UploadBatchDto>(sql, new { BatchId = batchId });
    }

    public async Task<IEnumerable<UploadBatchDto>> GetBatchesPagedAsync(long tenantId, int page, int pageSize)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT b.BatchId, b.TenantId, b.UploadTypeId, t.Code AS TypeCode, t.Name AS TypeName, b.FileName,
                   b.TotalRows, b.SuccessRows, b.ErrorRows, b.Status, b.ErrorMessage, b.CreatedOn, b.CompletedOn
            FROM dbo.UploadBatch b
            INNER JOIN dbo.UploadType t ON b.UploadTypeId = t.Id
            WHERE b.TenantId = @TenantId
            ORDER BY b.CreatedOn DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        return await conn.QueryAsync<UploadBatchDto>(sql, new
        {
            TenantId = tenantId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });
    }

    public async Task<(int TotalCount, IEnumerable<BatchRowResultDto> Rows)> GetBatchRowsAsync(Guid batchId, string? status, int page, int pageSize)
    {
        using var conn = GetConnection();
        var batch = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT b.UploadTypeId, t.StageTable FROM dbo.UploadBatch b JOIN dbo.UploadType t ON b.UploadTypeId = t.Id WHERE b.BatchId = @BatchId",
            new { BatchId = batchId });

        if (batch == null) return (0, Enumerable.Empty<BatchRowResultDto>());

        string stageTable = batch.StageTable;
        string statusFilter = string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase) 
            ? "" 
            : "AND RowStatus = @Status";

        string countSql = $"SELECT COUNT(1) FROM dbo.{stageTable} WHERE BatchId = @BatchId {statusFilter}";
        int total = await conn.ExecuteScalarAsync<int>(countSql, new { BatchId = batchId, Status = status });

        string fetchSql = $@"
            SELECT * FROM dbo.{stageTable}
            WHERE BatchId = @BatchId {statusFilter}
            ORDER BY RowNo
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dynamicRows = await conn.QueryAsync(fetchSql, new
        {
            BatchId = batchId,
            Status = status,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        var results = new List<BatchRowResultDto>();
        foreach (IDictionary<string, object> row in dynamicRows)
        {
            var dto = new BatchRowResultDto
            {
                RowNo = Convert.ToInt32(row["RowNo"]),
                Status = row["RowStatus"]?.ToString() ?? "Unknown",
                Message = row["ErrorMessage"]?.ToString(),
                KeyValue = row.ContainsKey("Name") ? row["Name"]?.ToString() ?? "" : (row.ContainsKey("ItemName") ? row["ItemName"]?.ToString() ?? "" : "")
            };

            foreach (var kvp in row)
            {
                if (kvp.Key is not "BatchId" and not "RowNo" and not "RowStatus" and not "ErrorMessage")
                {
                    dto.Values[kvp.Key] = kvp.Value;
                }
            }
            results.Add(dto);
        }

        return (total, results);
    }

    public async Task<byte[]> GenerateBatchReportAsync(Guid batchId)
    {
        using var conn = GetConnection();
        var batch = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT b.UploadTypeId, t.StageTable FROM dbo.UploadBatch b JOIN dbo.UploadType t ON b.UploadTypeId = t.Id WHERE b.BatchId = @BatchId",
            new { BatchId = batchId });

        if (batch == null) return Array.Empty<byte>();

        string stageTable = batch.StageTable;
        var rows = (await conn.QueryAsync($"SELECT * FROM dbo.{stageTable} WHERE BatchId = @BatchId ORDER BY RowNo", new { BatchId = batchId })).ToList();

        using var ms = new MemoryStream();
        var reportRows = new List<Dictionary<string, object?>>();

        foreach (IDictionary<string, object> r in rows)
        {
            var dict = new Dictionary<string, object?>
            {
                ["Row #"] = r["RowNo"],
                ["Status"] = r["RowStatus"],
                ["Validation Message"] = r["ErrorMessage"] ?? "Success"
            };

            foreach (var kvp in r)
            {
                if (kvp.Key is not "BatchId" and not "RowNo" and not "RowStatus" and not "ErrorMessage")
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }
            reportRows.Add(dict);
        }

        await ms.SaveAsAsync(reportRows, sheetName: "Upload_Report");
        return ms.ToArray();
    }
}
