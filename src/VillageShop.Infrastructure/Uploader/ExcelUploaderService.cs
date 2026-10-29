using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    Task<byte[]> GenerateTemplateAsync(string typeCode, long tenantId);
    Task<Guid> StageExcelAsync(string typeCode, Stream fileStream, string fileName, long tenantId, long userId, CancellationToken ct);
    Task<bool> QueueProcessingAsync(Guid batchId, long tenantId, long userId);
    Task<UploadBatchDto?> GetBatchStatusAsync(Guid batchId, long tenantId);
    Task<IEnumerable<UploadBatchDto>> GetBatchesPagedAsync(long tenantId, int page, int pageSize);
    Task<(int TotalCount, IEnumerable<BatchRowResultDto> Rows)> GetBatchRowsAsync(Guid batchId, long tenantId, string? status, int page, int pageSize);
    Task<byte[]> GenerateBatchReportAsync(Guid batchId, long tenantId);
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
            SELECT Id, Code, GroupType, Name, StageTable, MainTable FROM dbo.V_UploadType WHERE IsActive = 1 ORDER BY SortOrder;
            SELECT Id, UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder
            FROM dbo.V_UploadColumn ORDER BY SortOrder;";

        using var multi = await conn.QueryMultipleAsync(sql);
        var types = (await multi.ReadAsync<UploadTypeDto>()).ToList();
        var columns = (await multi.ReadAsync<UploadColumnDto>()).ToList();

        foreach (var t in types)
        {
            t.Columns = columns.Where(c => c.UploadTypeId == t.Id).ToList();
        }
        return types;
    }

    public async Task<byte[]> GenerateTemplateAsync(string typeCode, long tenantId)
    {
        var types = await GetActiveTypesAsync();
        var type = types.FirstOrDefault(t => t.Code.Equals(typeCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Upload type '{typeCode}' not found.");

        using var memoryStream = new MemoryStream();
        var rows = new List<Dictionary<string, object?>>();

        if (type.Code.Equals("Transaction", StringComparison.OrdinalIgnoreCase))
        {
            // Rich multi-type sample rows for common transaction
            rows.Add(new Dictionary<string, object?>
            {
                ["TransactionType*"] = "Stock",
                ["DocumentNumber"] = "STK-2026-001",
                ["TransactionDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["ItemCode"] = "ITM-TEA-001",
                ["ItemName*"] = "Tata Tea Gold 250g",
                ["ItemType"] = "Packed",
                ["Category"] = "Beverages",
                ["Brand"] = "Tata",
                ["Unit"] = "pcs",
                ["Barcode"] = "8901234567890",
                ["Quantity*"] = 50,
                ["Rate*"] = 140.00,
                ["Discount"] = 0.00,
                ["Tax"] = 5.00,
                ["BatchNumber"] = "B-2026-01",
                ["ExpiryDate"] = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
                ["CustomerOrSupplier"] = "Direct Warehouse",
                ["PaymentMode"] = "Cash",
                ["Remarks"] = "Opening stock load"
            });

            rows.Add(new Dictionary<string, object?>
            {
                ["TransactionType*"] = "Sale",
                ["DocumentNumber"] = "INV-2026-001",
                ["TransactionDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["ItemCode"] = "ITM-TEA-001",
                ["ItemName*"] = "Tata Tea Gold 250g",
                ["ItemType"] = "Packed",
                ["Category"] = "Beverages",
                ["Brand"] = "Tata",
                ["Unit"] = "pcs",
                ["Barcode"] = "8901234567890",
                ["Quantity*"] = 2,
                ["Rate*"] = 150.00,
                ["Discount"] = 0.00,
                ["Tax"] = 5.00,
                ["BatchNumber"] = "B-2026-01",
                ["ExpiryDate"] = "",
                ["CustomerOrSupplier"] = "Rajesh Verma",
                ["PaymentMode"] = "Cash",
                ["Remarks"] = "Counter cash sale"
            });

            rows.Add(new Dictionary<string, object?>
            {
                ["TransactionType*"] = "Purchase",
                ["DocumentNumber"] = "PO-2026-001",
                ["TransactionDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["ItemCode"] = "ITM-SUGAR-001",
                ["ItemName*"] = "Madhur Pure Sugar 1kg",
                ["ItemType"] = "Packed",
                ["Category"] = "Grocery",
                ["Brand"] = "Madhur",
                ["Unit"] = "kg",
                ["Barcode"] = "8901234567891",
                ["Quantity*"] = 100,
                ["Rate*"] = 42.00,
                ["Discount"] = 0.00,
                ["Tax"] = 0.00,
                ["BatchNumber"] = "B-SUGAR-01",
                ["ExpiryDate"] = DateTime.UtcNow.AddMonths(18).ToString("yyyy-MM-dd"),
                ["CustomerOrSupplier"] = "Gupta Wholesale Traders",
                ["PaymentMode"] = "UPI",
                ["Remarks"] = "Weekly inventory restock"
            });
        }
        else
        {
            var sampleRow = new Dictionary<string, object?>();
            foreach (var col in type.Columns.OrderBy(c => c.SortOrder))
            {
                sampleRow[col.ExcelHeader] = col.SampleValue ?? (col.IsRequired ? "Required" : "Optional");
            }
            rows.Add(sampleRow);
        }

        await memoryStream.SaveAsAsync(rows, sheetName: type.Code);
        return memoryStream.ToArray();
    }

    private static string NormalizeKey(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var chars = text.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToLowerInvariant();
    }

    private static string? CleanNumeric(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var clean = Regex.Replace(raw, @"[^\d.-]", "");
        return string.IsNullOrWhiteSpace(clean) ? null : clean;
    }

    public async Task<Guid> StageExcelAsync(string typeCode, Stream fileStream, string fileName, long tenantId, long userId, CancellationToken ct)
    {
        var types = await GetActiveTypesAsync();
        var type = types.FirstOrDefault(t => t.Code.Equals(typeCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Type '{typeCode}' is not active.");

        var batchId = Guid.NewGuid();
        var columns = type.Columns.OrderBy(c => c.SortOrder).ToList();

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        using var dataTable = new DataTable();
        dataTable.Columns.Add("BatchId", typeof(Guid));
        dataTable.Columns.Add("RowNo", typeof(int));
        dataTable.Columns.Add("TenantId", typeof(long));
        foreach (var col in columns)
        {
            dataTable.Columns.Add(col.DbColumn, typeof(string));
        }
        dataTable.Columns.Add("RowStatus", typeof(string));
        dataTable.Columns.Add("ErrorMessage", typeof(string));

        var excelRows = memoryStream.Query(useHeaderRow: true).ToList();
        if (!excelRows.Any())
        {
            throw new InvalidOperationException("The uploaded Excel workbook contains no data rows.");
        }

        // Build mapping from Excel row keys to column definitions using normalized keys
        var firstRow = (IDictionary<string, object>)excelRows.First();
        var keyMapping = new Dictionary<UploadColumnDto, string>();

        foreach (var col in columns)
        {
            var normCol = NormalizeKey(col.ExcelHeader);
            var matchedKey = firstRow.Keys.FirstOrDefault(k => NormalizeKey(k) == normCol);
            if (matchedKey != null)
            {
                keyMapping[col] = matchedKey;
            }
        }

        int rowNumber = 1;
        foreach (IDictionary<string, object> row in excelRows)
        {
            ct.ThrowIfCancellationRequested();
            var dataRow = dataTable.NewRow();
            dataRow["BatchId"] = batchId;
            dataRow["RowNo"] = rowNumber++;
            dataRow["TenantId"] = tenantId;

            bool rowHasData = false;
            foreach (var col in columns)
            {
                string? rawVal = null;
                if (keyMapping.TryGetValue(col, out var matchedKey) && row.TryGetValue(matchedKey, out var val) && val != null)
                {
                    rawVal = val.ToString()?.Trim();
                }

                if (!string.IsNullOrWhiteSpace(rawVal))
                {
                    rowHasData = true;
                    if (col.DataType.Equals("DECIMAL", StringComparison.OrdinalIgnoreCase) || 
                        col.DataType.Equals("INT", StringComparison.OrdinalIgnoreCase))
                    {
                        rawVal = CleanNumeric(rawVal);
                    }
                }

                dataRow[col.DbColumn] = string.IsNullOrWhiteSpace(rawVal) ? DBNull.Value : rawVal;
            }

            if (rowHasData)
            {
                dataRow["RowStatus"] = "Uploaded";
                dataRow["ErrorMessage"] = DBNull.Value;
                dataTable.Rows.Add(dataRow);
            }
        }

        int totalRows = dataTable.Rows.Count;
        if (totalRows == 0)
        {
            throw new InvalidOperationException("No valid records found in the uploaded file.");
        }

        using var conn = GetConnection();
        await conn.OpenAsync(ct);
        using var tx = conn.BeginTransaction();

        const string insertBatchSql = @"
            INSERT INTO dbo.V_UploadBatch (BatchId, TenantId, UploadTypeId, FileName, TotalRows, SuccessRows, ErrorRows, Status, UploadedBy, CreatedOn)
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

        using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints, tx)
        {
            DestinationTableName = $"dbo.{type.StageTable}",
            BatchSize = 10000,
            BulkCopyTimeout = 300
        };

        bulkCopy.ColumnMappings.Add("BatchId", "BatchId");
        bulkCopy.ColumnMappings.Add("RowNo", "RowNo");
        bulkCopy.ColumnMappings.Add("TenantId", "TenantId");
        foreach (var col in columns)
        {
            bulkCopy.ColumnMappings.Add(col.DbColumn, col.DbColumn);
        }
        bulkCopy.ColumnMappings.Add("RowStatus", "RowStatus");
        bulkCopy.ColumnMappings.Add("ErrorMessage", "ErrorMessage");

        await bulkCopy.WriteToServerAsync(dataTable, ct);
        tx.Commit();
        return batchId;
    }

    public async Task<bool> QueueProcessingAsync(Guid batchId, long tenantId, long userId)
    {
        using var conn = GetConnection();
        var status = await conn.ExecuteScalarAsync<string>(
            "SELECT Status FROM dbo.V_UploadBatch WHERE BatchId = @BatchId AND TenantId = @TenantId", 
            new { BatchId = batchId, TenantId = tenantId });

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
            FROM dbo.V_UploadBatch b
            INNER JOIN dbo.V_UploadType t ON b.UploadTypeId = t.Id
            WHERE b.BatchId = @BatchId AND b.TenantId = @TenantId", new { BatchId = batchId, TenantId = tenantId });

        if (batch == null || batch.Status == "Validating" || batch.Status == "Syncing" || batch.Status == "Completed")
            return;

        try
        {
            await conn.ExecuteAsync("UPDATE dbo.V_UploadBatch SET Status = 'Validating' WHERE BatchId = @BatchId AND TenantId = @TenantId", new { BatchId = batchId, TenantId = tenantId });

            var columns = (await conn.QueryAsync<UploadColumnDto>(
                "SELECT * FROM dbo.V_UploadColumn WHERE UploadTypeId = @UploadTypeId ORDER BY SortOrder",
                new { batch.UploadTypeId })).ToList();

            var validationSql = BuildSetBasedValidationSql(batch.StageTable, batch.TypeCode, batchId, tenantId, columns);
            await conn.ExecuteAsync(validationSql, new { BatchId = batchId, TenantId = tenantId }, commandTimeout: 300);

            // Check if any errors occurred
            var errorCount = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM dbo.{batch.StageTable} WHERE BatchId = @BatchId AND TenantId = @TenantId AND RowStatus = 'Error'",
                new { BatchId = batchId, TenantId = tenantId });

            await conn.ExecuteAsync("UPDATE dbo.V_UploadBatch SET Status = 'Syncing' WHERE BatchId = @BatchId AND TenantId = @TenantId", new { BatchId = batchId, TenantId = tenantId });

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
                FROM dbo.{0} WHERE BatchId = @BatchId AND TenantId = @TenantId;

                UPDATE dbo.V_UploadBatch
                SET Status = 'Completed',
                    SuccessRows = @Success,
                    ErrorRows = @Errors,
                    CompletedOn = SYSUTCDATETIME()
                WHERE BatchId = @BatchId AND TenantId = @TenantId;";

            await conn.ExecuteAsync(string.Format(summarizeSql, batch.StageTable), new { BatchId = batchId, TenantId = tenantId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Upload Batch {BatchId} for Tenant {TenantId}", batchId, tenantId);
            await conn.ExecuteAsync(@"
                UPDATE dbo.V_UploadBatch 
                SET Status = 'Failed', ErrorMessage = @ErrorMessage, CompletedOn = SYSUTCDATETIME() 
                WHERE BatchId = @BatchId AND TenantId = @TenantId",
                new { BatchId = batchId, TenantId = tenantId, ErrorMessage = ex.Message });
        }
    }

    private string BuildSetBasedValidationSql(string stageTable, string typeCode, Guid batchId, long tenantId, List<UploadColumnDto> columns)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"UPDATE dbo.{stageTable} SET RowStatus = 'Valid', ErrorMessage = NULL WHERE BatchId = @BatchId AND TenantId = @TenantId;");

        // Specific business rule validations for Common Transaction
        if (typeCode.Equals("Transaction", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), 'Invalid TransactionType. Allowed: Stock, Sale, Purchase, SaleReturn, PurchaseReturn.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND s.TenantId = @TenantId
                  AND (s.TransactionType IS NULL OR LOWER(LTRIM(RTRIM(s.TransactionType))) NOT IN ('stock', 'openingstock', 'sale', 'purchase', 'salereturn', 'purchasereturn'));");

            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), 'Quantity must be a positive number greater than 0.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND s.TenantId = @TenantId
                  AND (s.Quantity IS NULL OR TRY_CAST(s.Quantity AS DECIMAL(18,3)) IS NULL OR TRY_CAST(s.Quantity AS DECIMAL(18,3)) <= 0);");

            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), 'Rate must be a non-negative number.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND s.TenantId = @TenantId
                  AND (s.Rate IS NULL OR TRY_CAST(s.Rate AS DECIMAL(18,2)) IS NULL OR TRY_CAST(s.Rate AS DECIMAL(18,2)) < 0);");
        }

        // Standard column constraints
        foreach (var col in columns.Where(c => c.IsRequired))
        {
            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), '{col.ExcelHeader} is required.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND s.TenantId = @TenantId AND (s.{col.DbColumn} IS NULL OR LTRIM(RTRIM(s.{col.DbColumn})) = '');");
        }

        foreach (var col in columns.Where(c => c.MaxLength.HasValue && c.DataType.Equals("NVARCHAR", StringComparison.OrdinalIgnoreCase)))
        {
            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), '{col.ExcelHeader} exceeds maximum length of {col.MaxLength}.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND s.TenantId = @TenantId AND s.{col.DbColumn} IS NOT NULL AND LEN(s.{col.DbColumn}) > {col.MaxLength};");
        }

        foreach (var col in columns.Where(c => c.DataType.Equals("DECIMAL", StringComparison.OrdinalIgnoreCase) || c.DataType.Equals("INT", StringComparison.OrdinalIgnoreCase)))
        {
            sb.AppendLine($@"
                UPDATE s
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), '{col.ExcelHeader} must be a valid numeric amount.')
                FROM dbo.{stageTable} s
                WHERE s.BatchId = @BatchId AND s.TenantId = @TenantId AND s.{col.DbColumn} IS NOT NULL AND LTRIM(RTRIM(s.{col.DbColumn})) <> '' AND TRY_CAST(s.{col.DbColumn} AS DECIMAL(18,4)) IS NULL;");
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
                    WHERE BatchId = @BatchId AND TenantId = @TenantId
                    GROUP BY {keyGroup}
                    HAVING COUNT(1) > 1
                )
                UPDATE t
                SET RowStatus = 'Error',
                    ErrorMessage = CONCAT(ISNULL(ErrorMessage + ' | ', ''), 'Duplicate key detected in upload file.')
                FROM dbo.{stageTable} t
                INNER JOIN DupKeys d ON {keyJoin}
                WHERE t.BatchId = @BatchId AND t.TenantId = @TenantId;");
        }

        return sb.ToString();
    }

    public async Task<UploadBatchDto?> GetBatchStatusAsync(Guid batchId, long tenantId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT b.BatchId, b.TenantId, b.UploadTypeId, t.Code AS TypeCode, t.GroupType, t.Name AS TypeName, b.FileName,
                   b.TotalRows, b.SuccessRows, b.ErrorRows, b.Status, b.ErrorMessage, b.CreatedOn, b.CompletedOn
            FROM dbo.V_UploadBatch b
            INNER JOIN dbo.V_UploadType t ON b.UploadTypeId = t.Id
            WHERE b.BatchId = @BatchId AND b.TenantId = @TenantId";
        return await conn.QueryFirstOrDefaultAsync<UploadBatchDto>(sql, new { BatchId = batchId, TenantId = tenantId });
    }

    public async Task<IEnumerable<UploadBatchDto>> GetBatchesPagedAsync(long tenantId, int page, int pageSize)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT b.BatchId, b.TenantId, b.UploadTypeId, t.Code AS TypeCode, t.GroupType, t.Name AS TypeName, b.FileName,
                   b.TotalRows, b.SuccessRows, b.ErrorRows, b.Status, b.ErrorMessage, b.CreatedOn, b.CompletedOn
            FROM dbo.V_UploadBatch b
            INNER JOIN dbo.V_UploadType t ON b.UploadTypeId = t.Id
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

    public async Task<(int TotalCount, IEnumerable<BatchRowResultDto> Rows)> GetBatchRowsAsync(Guid batchId, long tenantId, string? status, int page, int pageSize)
    {
        using var conn = GetConnection();
        var batch = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT b.UploadTypeId, t.StageTable FROM dbo.V_UploadBatch b JOIN dbo.V_UploadType t ON b.UploadTypeId = t.Id WHERE b.BatchId = @BatchId AND b.TenantId = @TenantId",
            new { BatchId = batchId, TenantId = tenantId });

        if (batch == null) return (0, Enumerable.Empty<BatchRowResultDto>());

        string stageTable = batch.StageTable;
        string statusFilter = string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase) 
            ? "" 
            : "AND RowStatus = @Status";

        string countSql = $"SELECT COUNT(1) FROM dbo.{stageTable} WHERE BatchId = @BatchId AND TenantId = @TenantId {statusFilter}";
        int total = await conn.ExecuteScalarAsync<int>(countSql, new { BatchId = batchId, TenantId = tenantId, Status = status });

        string fetchSql = $@"
            SELECT * FROM dbo.{stageTable}
            WHERE BatchId = @BatchId AND TenantId = @TenantId {statusFilter}
            ORDER BY RowNo
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dynamicRows = await conn.QueryAsync(fetchSql, new
        {
            BatchId = batchId,
            TenantId = tenantId,
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
                KeyValue = row.ContainsKey("ItemName") && row["ItemName"] != null ? row["ItemName"]?.ToString() ?? "" :
                          (row.ContainsKey("Name") && row["Name"] != null ? row["Name"]?.ToString() ?? "" : "")
            };

            foreach (var kvp in row)
            {
                if (kvp.Key is not "BatchId" and not "RowNo" and not "RowStatus" and not "ErrorMessage" and not "TenantId" and not "Id")
                {
                    dto.Values[kvp.Key] = kvp.Value;
                }
            }
            results.Add(dto);
        }

        return (total, results);
    }

    public async Task<byte[]> GenerateBatchReportAsync(Guid batchId, long tenantId)
    {
        using var conn = GetConnection();
        var batch = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT b.UploadTypeId, t.StageTable FROM dbo.V_UploadBatch b JOIN dbo.V_UploadType t ON b.UploadTypeId = t.Id WHERE b.BatchId = @BatchId AND b.TenantId = @TenantId",
            new { BatchId = batchId, TenantId = tenantId });

        if (batch == null) return Array.Empty<byte>();

        string stageTable = batch.StageTable;
        var rows = (await conn.QueryAsync($"SELECT * FROM dbo.{stageTable} WHERE BatchId = @BatchId AND TenantId = @TenantId ORDER BY RowNo", new { BatchId = batchId, TenantId = tenantId })).ToList();

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
                if (kvp.Key is not "BatchId" and not "RowNo" and not "RowStatus" and not "ErrorMessage" and not "TenantId" and not "Id")
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
