-- ============================================================================
-- SCRIPT: METADATA-DRIVEN EXCEL UPLOADER SCHEMA & STORED PROCEDURES
-- ============================================================================

USE [dev.inhouse];
GO

-- 1. CONFIGURATION TABLES
IF OBJECT_ID('dbo.UploadColumn', 'U') IS NOT NULL DROP TABLE dbo.UploadColumn;
IF OBJECT_ID('dbo.UploadBatch', 'U') IS NOT NULL DROP TABLE dbo.UploadBatch;
IF OBJECT_ID('dbo.UploadType', 'U') IS NOT NULL DROP TABLE dbo.UploadType;
GO

CREATE TABLE dbo.UploadType (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    StageTable VARCHAR(100) NOT NULL,
    MainTable VARCHAR(100) NOT NULL,
    SyncProcName VARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.UploadColumn (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UploadTypeId INT NOT NULL FOREIGN KEY REFERENCES dbo.UploadType(Id) ON DELETE CASCADE,
    ExcelHeader NVARCHAR(100) NOT NULL,
    DbColumn VARCHAR(100) NOT NULL,
    DataType VARCHAR(20) NOT NULL, -- NVARCHAR, INT, DECIMAL, DATE, BIT
    IsRequired BIT NOT NULL DEFAULT 0,
    MaxLength INT NULL,
    IsKey BIT NOT NULL DEFAULT 0,
    LookupTable VARCHAR(100) NULL,
    LookupColumn VARCHAR(100) NULL,
    DefaultValue NVARCHAR(200) NULL,
    SampleValue NVARCHAR(200) NULL,
    SortOrder INT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.UploadBatch (
    BatchId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId BIGINT NOT NULL DEFAULT 1,
    UploadTypeId INT NOT NULL FOREIGN KEY REFERENCES dbo.UploadType(Id),
    FileName NVARCHAR(255) NOT NULL,
    TotalRows INT NOT NULL DEFAULT 0,
    SuccessRows INT NOT NULL DEFAULT 0,
    ErrorRows INT NOT NULL DEFAULT 0,
    Status VARCHAR(20) NOT NULL, -- Uploaded, Validating, Syncing, Completed, Failed
    ErrorMessage NVARCHAR(MAX) NULL,
    UploadedBy BIGINT NOT NULL DEFAULT 0,
    CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedOn DATETIME2 NULL
);
CREATE NONCLUSTERED INDEX IX_UploadBatch_Tenant_Status ON dbo.UploadBatch(TenantId, Status, CreatedOn DESC);
GO

-- 2. STAGE TABLES (Row-wise validation & isolation)
IF OBJECT_ID('dbo.Stg_ItemCategory', 'U') IS NOT NULL DROP TABLE dbo.Stg_ItemCategory;
CREATE TABLE dbo.Stg_ItemCategory (
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    Name NVARCHAR(200) NULL,
    Description NVARCHAR(500) NULL,
    RowStatus VARCHAR(10) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL,
    PRIMARY KEY CLUSTERED (BatchId, RowNo)
);
CREATE NONCLUSTERED INDEX IX_Stg_ItemCategory_Status ON dbo.Stg_ItemCategory (BatchId, RowStatus) INCLUDE (RowNo);
GO

IF OBJECT_ID('dbo.Stg_ItemType', 'U') IS NOT NULL DROP TABLE dbo.Stg_ItemType;
CREATE TABLE dbo.Stg_ItemType (
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    Name NVARCHAR(200) NULL,
    CategoryName NVARCHAR(200) NULL,
    Description NVARCHAR(500) NULL,
    RowStatus VARCHAR(10) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL,
    PRIMARY KEY CLUSTERED (BatchId, RowNo)
);
CREATE NONCLUSTERED INDEX IX_Stg_ItemType_Status ON dbo.Stg_ItemType (BatchId, RowStatus) INCLUDE (RowNo);
GO

IF OBJECT_ID('dbo.Stg_Brand', 'U') IS NOT NULL DROP TABLE dbo.Stg_Brand;
CREATE TABLE dbo.Stg_Brand (
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    Name NVARCHAR(200) NULL,
    Manufacturer NVARCHAR(200) NULL,
    RowStatus VARCHAR(10) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL,
    PRIMARY KEY CLUSTERED (BatchId, RowNo)
);
CREATE NONCLUSTERED INDEX IX_Stg_Brand_Status ON dbo.Stg_Brand (BatchId, RowStatus) INCLUDE (RowNo);
GO

IF OBJECT_ID('dbo.Stg_Customer', 'U') IS NOT NULL DROP TABLE dbo.Stg_Customer;
CREATE TABLE dbo.Stg_Customer (
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    Name NVARCHAR(200) NULL,
    Mobile NVARCHAR(50) NULL,
    Village NVARCHAR(200) NULL,
    Address NVARCHAR(500) NULL,
    OpeningBalance NVARCHAR(50) NULL,
    CreditLimit NVARCHAR(50) NULL,
    RowStatus VARCHAR(10) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL,
    PRIMARY KEY CLUSTERED (BatchId, RowNo)
);
CREATE NONCLUSTERED INDEX IX_Stg_Customer_Status ON dbo.Stg_Customer (BatchId, RowStatus) INCLUDE (RowNo);
GO

IF OBJECT_ID('dbo.Stg_Stock', 'U') IS NOT NULL DROP TABLE dbo.Stg_Stock;
CREATE TABLE dbo.Stg_Stock (
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    ItemName NVARCHAR(200) NULL,
    Barcode NVARCHAR(100) NULL,
    Quantity NVARCHAR(50) NULL,
    PurchasePrice NVARCHAR(50) NULL,
    SellingPrice NVARCHAR(50) NULL,
    MRP NVARCHAR(50) NULL,
    MinStock NVARCHAR(50) NULL,
    RowStatus VARCHAR(10) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL,
    PRIMARY KEY CLUSTERED (BatchId, RowNo)
);
CREATE NONCLUSTERED INDEX IX_Stg_Stock_Status ON dbo.Stg_Stock (BatchId, RowStatus) INCLUDE (RowNo);
GO

-- 3. SEED METADATA CONFIGURATION
SET IDENTITY_INSERT dbo.UploadType ON;
INSERT INTO dbo.UploadType (Id, Code, Name, StageTable, MainTable, SyncProcName, IsActive, SortOrder) VALUES
(1, 'ItemCategory', N'Item Categories (वर्ग)', 'Stg_ItemCategory', 'V_Masters', 'usp_Sync_ItemCategory', 1, 1),
(2, 'ItemType', N'Item Sub-Categories (प्रकार)', 'Stg_ItemType', 'V_Masters', 'usp_Sync_ItemType', 1, 2),
(3, 'Brand', N'Brands (ब्रांड)', 'Stg_Brand', 'V_Masters', 'usp_Sync_Brand', 1, 3),
(4, 'Customer', N'Customers (ग्राहक)', 'Stg_Customer', 'V_Customers', 'usp_Sync_Customer', 1, 4),
(5, 'Stock', N'Stock & Inventory (स्टॉक इन्वेंटरी)', 'Stg_Stock', 'V_Stock', 'usp_Sync_Stock', 1, 5);
SET IDENTITY_INSERT dbo.UploadType OFF;
GO

-- Columns: ItemCategory
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(1, 'Category Name*', 'Name', 'NVARCHAR', 1, 150, 1, 'Grocery & Provisions', 1),
(1, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'All daily FMCG packaged goods', 2);

-- Columns: ItemType
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, LookupTable, LookupColumn, SampleValue, SortOrder) VALUES
(2, 'Sub-Category Name*', 'Name', 'NVARCHAR', 1, 150, 1, NULL, NULL, 'Spices & Masala', 1),
(2, 'Parent Category*', 'CategoryName', 'NVARCHAR', 1, 150, 0, 'V_Masters', 'Name', 'Grocery & Provisions', 2),
(2, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, NULL, NULL, 'Cooking condiments and powders', 3);

-- Columns: Brand
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(3, 'Brand Name*', 'Name', 'NVARCHAR', 1, 150, 1, 'Tata Consumer Products', 1),
(3, 'Manufacturer', 'Manufacturer', 'NVARCHAR', 0, 150, 0, 'Tata Group', 2);

-- Columns: Customer
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(4, 'Customer Name*', 'Name', 'NVARCHAR', 1, 150, 0, 'Rajesh Kumar Verma', 1),
(4, 'Mobile Number*', 'Mobile', 'NVARCHAR', 1, 15, 1, '9876543210', 2),
(4, 'Village / Area', 'Village', 'NVARCHAR', 0, 100, 0, 'Bishungarh', 3),
(4, 'Full Address', 'Address', 'NVARCHAR', 0, 300, 0, 'Main Market Ward 4', 4),
(4, 'Opening Udhaar (₹)', 'OpeningBalance', 'DECIMAL', 0, NULL, 0, '500.00', 5),
(4, 'Credit Limit (₹)', 'CreditLimit', 'DECIMAL', 0, NULL, 0, '10000.00', 6);

-- Columns: Stock
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, LookupTable, LookupColumn, SampleValue, SortOrder) VALUES
(5, 'Item Name*', 'ItemName', 'NVARCHAR', 1, 150, 1, 'V_Items', 'Name', 'Tata Salt 1kg', 1),
(5, 'Barcode / SKU', 'Barcode', 'NVARCHAR', 0, 50, 0, NULL, NULL, '8901234567890', 2),
(5, 'Stock Quantity*', 'Quantity', 'DECIMAL', 1, NULL, 0, NULL, NULL, '50', 3),
(5, 'Purchase Price (₹)*', 'PurchasePrice', 'DECIMAL', 1, NULL, 0, NULL, NULL, '22.50', 4),
(5, 'Selling Price (₹)*', 'SellingPrice', 'DECIMAL', 1, NULL, 0, NULL, NULL, '28.00', 5),
(5, 'MRP (₹)', 'MRP', 'DECIMAL', 0, NULL, 0, NULL, NULL, '30.00', 6),
(5, 'Minimum Alert Stock', 'MinStock', 'DECIMAL', 0, NULL, 0, NULL, NULL, '10', 7);
GO

-- 4. HIGH-PERFORMANCE SYNC STORED PROCEDURES (5k Batched Transactions)

CREATE OR ALTER PROCEDURE dbo.usp_Sync_ItemCategory
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BatchSize INT = 5000;
    DECLARE @RowsAffected INT = 1;

    WHILE (@RowsAffected > 0)
    BEGIN
        BEGIN TRANSACTION;

        WITH CTE AS (
            SELECT TOP (@BatchSize) s.RowNo, s.Name, s.Description
            FROM dbo.Stg_ItemCategory s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
            ORDER BY s.RowNo
        )
        MERGE INTO dbo.V_Masters AS target
        USING CTE AS source
        ON (target.TenantId = @TenantId AND target.MasterType = 'CATEGORY' AND LOWER(RTRIM(LTRIM(target.MasterName))) = LOWER(RTRIM(LTRIM(source.Name))) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET 
                target.Description = ISNULL(source.Description, target.Description),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED THEN
            INSERT (TenantId, MasterType, MasterName, Description, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
            VALUES (@TenantId, 'CATEGORY', RTRIM(LTRIM(source.Name)), source.Description, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME());

        SET @RowsAffected = @@ROWCOUNT;

        UPDATE s
        SET s.RowStatus = 'Synced'
        FROM dbo.Stg_ItemCategory s
        INNER JOIN (
            SELECT TOP (@BatchSize) RowNo 
            FROM dbo.Stg_ItemCategory 
            WHERE BatchId = @BatchId AND RowStatus = 'Valid' 
            ORDER BY RowNo
        ) b ON s.RowNo = b.RowNo
        WHERE s.BatchId = @BatchId;

        COMMIT TRANSACTION;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Sync_Brand
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BatchSize INT = 5000;
    DECLARE @RowsAffected INT = 1;

    WHILE (@RowsAffected > 0)
    BEGIN
        BEGIN TRANSACTION;

        WITH CTE AS (
            SELECT TOP (@BatchSize) s.RowNo, s.Name, s.Manufacturer
            FROM dbo.Stg_Brand s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
            ORDER BY s.RowNo
        )
        MERGE INTO dbo.V_Masters AS target
        USING CTE AS source
        ON (target.TenantId = @TenantId AND target.MasterType = 'BRAND' AND LOWER(RTRIM(LTRIM(target.MasterName))) = LOWER(RTRIM(LTRIM(source.Name))) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET 
                target.Description = ISNULL(source.Manufacturer, target.Description),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED THEN
            INSERT (TenantId, MasterType, MasterName, Description, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
            VALUES (@TenantId, 'BRAND', RTRIM(LTRIM(source.Name)), source.Manufacturer, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME());

        SET @RowsAffected = @@ROWCOUNT;

        UPDATE s
        SET s.RowStatus = 'Synced'
        FROM dbo.Stg_Brand s
        INNER JOIN (
            SELECT TOP (@BatchSize) RowNo 
            FROM dbo.Stg_Brand 
            WHERE BatchId = @BatchId AND RowStatus = 'Valid' 
            ORDER BY RowNo
        ) b ON s.RowNo = b.RowNo
        WHERE s.BatchId = @BatchId;

        COMMIT TRANSACTION;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Sync_Customer
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BatchSize INT = 5000;
    DECLARE @RowsAffected INT = 1;

    WHILE (@RowsAffected > 0)
    BEGIN
        BEGIN TRANSACTION;

        WITH CTE AS (
            SELECT TOP (@BatchSize) 
                s.RowNo, 
                RTRIM(LTRIM(s.Name)) AS Name, 
                RTRIM(LTRIM(s.Mobile)) AS Mobile, 
                s.Village, 
                s.Address,
                TRY_CAST(s.OpeningBalance AS DECIMAL(18,2)) AS OpeningBalance,
                TRY_CAST(s.CreditLimit AS DECIMAL(18,2)) AS CreditLimit
            FROM dbo.Stg_Customer s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
            ORDER BY s.RowNo
        )
        MERGE INTO dbo.V_Customers AS target
        USING CTE AS source
        ON (target.TenantId = @TenantId AND target.Mobile = source.Mobile AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET 
                target.Name = ISNULL(source.Name, target.Name),
                target.Village = ISNULL(source.Village, target.Village),
                target.Address = ISNULL(source.Address, target.Address),
                target.CreditLimit = ISNULL(source.CreditLimit, target.CreditLimit),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED THEN
            INSERT (TenantId, Name, Mobile, Village, Address, OpeningBalance, CurrentBalance, CreditLimit, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
            VALUES (@TenantId, source.Name, source.Mobile, source.Village, source.Address, ISNULL(source.OpeningBalance, 0), ISNULL(source.OpeningBalance, 0), ISNULL(source.CreditLimit, 5000), 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME());

        SET @RowsAffected = @@ROWCOUNT;

        UPDATE s
        SET s.RowStatus = 'Synced'
        FROM dbo.Stg_Customer s
        INNER JOIN (
            SELECT TOP (@BatchSize) RowNo 
            FROM dbo.Stg_Customer 
            WHERE BatchId = @BatchId AND RowStatus = 'Valid' 
            ORDER BY RowNo
        ) b ON s.RowNo = b.RowNo
        WHERE s.BatchId = @BatchId;

        COMMIT TRANSACTION;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Sync_Stock
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BatchSize INT = 5000;
    DECLARE @RowsAffected INT = 1;

    WHILE (@RowsAffected > 0)
    BEGIN
        BEGIN TRANSACTION;

        WITH CTE AS (
            SELECT TOP (@BatchSize)
                s.RowNo,
                i.ID AS ItemId,
                i.Name AS ItemName,
                s.Barcode,
                TRY_CAST(s.Quantity AS DECIMAL(18,3)) AS Quantity,
                TRY_CAST(s.PurchasePrice AS DECIMAL(18,2)) AS PurchasePrice,
                TRY_CAST(s.SellingPrice AS DECIMAL(18,2)) AS SellingPrice,
                TRY_CAST(s.MRP AS DECIMAL(18,2)) AS MRP,
                TRY_CAST(s.MinStock AS DECIMAL(18,3)) AS MinStock
            FROM dbo.Stg_Stock s
            INNER JOIN dbo.V_Items i ON i.TenantId = @TenantId AND LOWER(RTRIM(LTRIM(i.Name))) = LOWER(RTRIM(LTRIM(s.ItemName))) AND i.IsDeleted = 0
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
            ORDER BY s.RowNo
        )
        MERGE INTO dbo.V_Stock AS target
        USING CTE AS source
        ON (target.TenantId = @TenantId AND target.ItemId = source.ItemId AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.CurrentStock = target.CurrentStock + ISNULL(source.Quantity, 0),
                target.PurchasePrice = ISNULL(source.PurchasePrice, target.PurchasePrice),
                target.SellingPrice = ISNULL(source.SellingPrice, target.SellingPrice),
                target.MRP = ISNULL(source.MRP, target.MRP),
                target.MinimumStock = ISNULL(source.MinStock, target.MinimumStock),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED THEN
            INSERT (TenantId, ItemId, Name, Barcode, CurrentStock, OpeningStock, MinimumStock, PurchasePrice, SellingPrice, MRP, GSTPercent, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
            VALUES (@TenantId, source.ItemId, source.ItemName, source.Barcode, ISNULL(source.Quantity, 0), ISNULL(source.Quantity, 0), ISNULL(source.MinStock, 5), ISNULL(source.PurchasePrice, 0), ISNULL(source.SellingPrice, 0), ISNULL(source.MRP, 0), 0, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME());

        SET @RowsAffected = @@ROWCOUNT;

        UPDATE s
        SET s.RowStatus = 'Synced'
        FROM dbo.Stg_Stock s
        INNER JOIN (
            SELECT TOP (@BatchSize) RowNo 
            FROM dbo.Stg_Stock 
            WHERE BatchId = @BatchId AND RowStatus = 'Valid' 
            ORDER BY RowNo
        ) b ON s.RowNo = b.RowNo
        WHERE s.BatchId = @BatchId;

        COMMIT TRANSACTION;
    END
END;
GO
