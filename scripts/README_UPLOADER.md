# Generic Metadata-Driven Bulk Excel Uploader

A high-performance, metadata-driven Excel bulk uploader architecture built for ASP.NET Core (.NET 8) Web API and Flutter on Microsoft SQL Server.

---

## Architecture Overview

```
[Flutter Client] ──(.xlsx streaming)──> [/api/uploader/upload/{type}]
                                              │
                                              ▼
                                       [SqlBulkCopy (TABLOCK, 10k batch)]
                                              │
                                              ▼
                                       [Stage Table: Stg_{Type}]
                                              │
                                       (Background Channel Queue)
                                              ▼
                                       [Set-Based SQL Validation]
                                              │
                                              ▼
                                       [5,000-Row Batched Sync/MERGE into Main Table]
```

---

## How to Add a New Upload Type in 5 Steps (No Code Changes Required)

To onboard any new master or transactional data type (e.g., `Supplier`, `Expense`, `Warehouse`), follow these 5 configuration steps:

### Step 1: Register the Upload Type in `UploadType`
Insert a new record in `UploadType` mapping the unique code, display name, stage table, and target main table:
```sql
INSERT INTO UploadType (Code, Name, StageTable, MainTable, SyncProcName, IsActive, SortOrder)
VALUES ('Supplier', 'Supplier Master', 'Stg_Supplier', 'V_Suppliers', 'usp_Sync_Supplier', 1, 6);
```

### Step 2: Configure Column Definitions in `UploadColumn`
Define headers, target DB columns, validation constraints, and foreign key lookup rules:
```sql
DECLARE @TypeId INT = (SELECT Id FROM UploadType WHERE Code = 'Supplier');

INSERT INTO UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, LookupTable, LookupColumn, SortOrder)
VALUES 
(@TypeId, 'Supplier Code', 'Code', 'string', 1, 50, 1, NULL, NULL, 1),
(@TypeId, 'Supplier Name', 'Name', 'string', 1, 150, 0, NULL, NULL, 2),
(@TypeId, 'Mobile Number', 'Mobile', 'string', 0, 15, 0, NULL, NULL, 3),
(@TypeId, 'Email', 'Email', 'string', 0, 100, 0, NULL, NULL, 4),
(@TypeId, 'GST Number', 'GstNo', 'string', 0, 20, 0, NULL, NULL, 5);
```

### Step 3: Create the Staging Table
Create the staging table `Stg_{Type}` containing the metadata tracking columns and business columns:
```sql
CREATE TABLE Stg_Supplier (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    TenantId BIGINT NOT NULL,
    Code NVARCHAR(200),
    Name NVARCHAR(200),
    Mobile NVARCHAR(200),
    Email NVARCHAR(200),
    GstNo NVARCHAR(200),
    RowStatus NVARCHAR(50) DEFAULT 'Pending',
    ErrorMessage NVARCHAR(MAX),
    CreatedOn DATETIME2 DEFAULT SYSUTCDATETIME()
);
CREATE NONCLUSTERED INDEX IX_Stg_Supplier_Batch ON Stg_Supplier(BatchId, RowStatus) INCLUDE (RowNo);
```

### Step 4: Create the Sync Stored Procedure
Create `usp_Sync_{Type}` to execute 5,000-row batched MERGE operations inside a transaction:
```sql
CREATE OR ALTER PROCEDURE usp_Sync_Supplier
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @BatchSize INT = 5000
AS
BEGIN
    SET NOCOUNT ON;
    
    WHILE (1 = 1)
    BEGIN
        ;WITH BatchToSync AS (
            SELECT TOP (@BatchSize) *
            FROM Stg_Supplier WITH (ROWLOCK, READPAST)
            WHERE BatchId = @BatchId AND RowStatus = 'Valid'
            ORDER BY Id
        )
        MERGE INTO V_Suppliers AS target
        USING BatchToSync AS src
        ON (target.TenantId = @TenantId AND target.SupplierCode = src.Code)
        WHEN MATCHED THEN
            UPDATE SET 
                target.SupplierName = src.Name,
                target.Mobile = src.Mobile,
                target.Email = src.Email,
                target.GstNo = src.GstNo,
                target.UpdatedOn = SYSUTCDATETIME()
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, SupplierCode, SupplierName, Mobile, Email, GstNo, CreatedOn)
            VALUES (@TenantId, src.Code, src.Name, src.Mobile, src.Email, src.GstNo, SYSUTCDATETIME())
        OUTPUT src.Id;

        UPDATE TOP (@BatchSize) Stg_Supplier
        SET RowStatus = 'Synced'
        WHERE BatchId = @BatchId AND RowStatus = 'Valid';

        IF @@ROWCOUNT = 0 BREAK;
    END;
END;
```

### Step 5: Refresh the Frontend
No code changes are needed in .NET or Flutter! 
- The Flutter client's dropdown instantly displays the new `Supplier Master` type fetched from `/api/uploader/types`.
- Clicking **Download Template** dynamically serves an `.xlsx` workbook formatted with required headers and sample data from `UploadColumn`.
- Uploaded files are streamed, bulk-copied, validated set-based, and synced via the new stored procedure.
