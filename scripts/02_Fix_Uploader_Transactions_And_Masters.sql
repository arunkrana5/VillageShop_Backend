-- ============================================================================
-- SCRIPT: COMPREHENSIVE FIX FOR EXCEL UPLOADER & DOWNLOADER
-- Single Common Transaction Template + Dedicated Master Templates + Tenant Isolation
-- ============================================================================

USE [dev.inhouse];
GO

-- 1. Ensure GroupType exists on UploadType
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UploadType') AND name = 'GroupType')
BEGIN
    ALTER TABLE dbo.UploadType ADD GroupType VARCHAR(20) NOT NULL DEFAULT 'Master';
END
GO

-- Ensure TenantId exists on staging tables
IF OBJECT_ID('dbo.Stg_Transaction', 'U') IS NOT NULL DROP TABLE dbo.Stg_Transaction;
CREATE TABLE dbo.Stg_Transaction (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    TenantId BIGINT NOT NULL DEFAULT 1,
    TransactionType NVARCHAR(50) NOT NULL,
    DocumentNumber NVARCHAR(100) NULL,
    TransactionDate NVARCHAR(50) NULL,
    ItemCode NVARCHAR(100) NULL,
    ItemName NVARCHAR(250) NOT NULL,
    ItemType NVARCHAR(150) NULL,
    Category NVARCHAR(150) NULL,
    Brand NVARCHAR(150) NULL,
    Unit NVARCHAR(50) NULL,
    Barcode NVARCHAR(100) NULL,
    Quantity NVARCHAR(50) NOT NULL,
    Rate NVARCHAR(50) NOT NULL,
    Discount NVARCHAR(50) NULL,
    Tax NVARCHAR(50) NULL,
    BatchNumber NVARCHAR(100) NULL,
    ExpiryDate NVARCHAR(50) NULL,
    CustomerOrSupplier NVARCHAR(200) NULL,
    PaymentMode NVARCHAR(50) NULL,
    Remarks NVARCHAR(500) NULL,
    RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL
);
CREATE NONCLUSTERED INDEX IX_Stg_Transaction_Batch ON dbo.Stg_Transaction(BatchId, RowStatus) INCLUDE (RowNo);
GO

-- Stg_Item for Dedicated Item/Product Master
IF OBJECT_ID('dbo.Stg_Item', 'U') IS NOT NULL DROP TABLE dbo.Stg_Item;
CREATE TABLE dbo.Stg_Item (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    TenantId BIGINT NOT NULL DEFAULT 1,
    ItemCode NVARCHAR(100) NULL,
    Name NVARCHAR(250) NOT NULL,
    Category NVARCHAR(150) NULL,
    Brand NVARCHAR(150) NULL,
    ItemType NVARCHAR(150) NULL,
    Unit NVARCHAR(50) NULL,
    Barcode NVARCHAR(100) NULL,
    PurchasePrice NVARCHAR(50) NULL,
    SellingPrice NVARCHAR(50) NULL,
    MRP NVARCHAR(50) NULL,
    MinStock NVARCHAR(50) NULL,
    Description NVARCHAR(500) NULL,
    RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL
);
CREATE NONCLUSTERED INDEX IX_Stg_Item_Batch ON dbo.Stg_Item(BatchId, RowStatus) INCLUDE (RowNo);
GO

-- Stg_Unit for Unit of Measurement Master
IF OBJECT_ID('dbo.Stg_Unit', 'U') IS NOT NULL DROP TABLE dbo.Stg_Unit;
CREATE TABLE dbo.Stg_Unit (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    TenantId BIGINT NOT NULL DEFAULT 1,
    Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(50) NOT NULL,
    Description NVARCHAR(300) NULL,
    RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL
);
CREATE NONCLUSTERED INDEX IX_Stg_Unit_Batch ON dbo.Stg_Unit(BatchId, RowStatus) INCLUDE (RowNo);
GO

-- Stg_Supplier for Supplier Master
IF OBJECT_ID('dbo.Stg_Supplier', 'U') IS NOT NULL DROP TABLE dbo.Stg_Supplier;
CREATE TABLE dbo.Stg_Supplier (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    TenantId BIGINT NOT NULL DEFAULT 1,
    Name NVARCHAR(200) NOT NULL,
    Mobile NVARCHAR(50) NULL,
    Address NVARCHAR(500) NULL,
    GSTNumber NVARCHAR(50) NULL,
    OpeningBalance NVARCHAR(50) NULL,
    RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL
);
CREATE NONCLUSTERED INDEX IX_Stg_Supplier_Batch ON dbo.Stg_Supplier(BatchId, RowStatus) INCLUDE (RowNo);
GO

-- Stg_Tax for Tax/GST Master
IF OBJECT_ID('dbo.Stg_Tax', 'U') IS NOT NULL DROP TABLE dbo.Stg_Tax;
CREATE TABLE dbo.Stg_Tax (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RowNo INT NOT NULL,
    TenantId BIGINT NOT NULL DEFAULT 1,
    Name NVARCHAR(100) NOT NULL,
    RatePercent NVARCHAR(50) NOT NULL,
    Description NVARCHAR(300) NULL,
    RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
    ErrorMessage NVARCHAR(1000) NULL
);
CREATE NONCLUSTERED INDEX IX_Stg_Tax_Batch ON dbo.Stg_Tax(BatchId, RowStatus) INCLUDE (RowNo);
GO

-- Ensure TenantId on existing stage tables
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stg_ItemCategory') AND name = 'TenantId')
    ALTER TABLE dbo.Stg_ItemCategory ADD TenantId BIGINT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stg_ItemType') AND name = 'TenantId')
    ALTER TABLE dbo.Stg_ItemType ADD TenantId BIGINT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stg_Brand') AND name = 'TenantId')
    ALTER TABLE dbo.Stg_Brand ADD TenantId BIGINT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stg_Customer') AND name = 'TenantId')
    ALTER TABLE dbo.Stg_Customer ADD TenantId BIGINT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stg_Stock') AND name = 'TenantId')
    ALTER TABLE dbo.Stg_Stock ADD TenantId BIGINT NOT NULL DEFAULT 1;
GO

-- 2. SEED CLEAN CONFIGURATION FOR TRANSACTIONS & MASTERS
-- Clean existing upload metadata
DELETE FROM dbo.UploadColumn;
DELETE FROM dbo.UploadType;
GO

SET IDENTITY_INSERT dbo.UploadType ON;

-- 1. COMMON TRANSACTION TEMPLATE
INSERT INTO dbo.UploadType (Id, Code, Name, StageTable, MainTable, SyncProcName, IsActive, SortOrder, GroupType)
VALUES (1, 'Transaction', N'Common Transaction (Stock, Sale, Purchase, Returns)', 'Stg_Transaction', 'V_Stock', 'usp_Sync_Transaction', 1, 1, 'Transaction');

-- 2. DEDICATED MASTER TEMPLATES
INSERT INTO dbo.UploadType (Id, Code, Name, StageTable, MainTable, SyncProcName, IsActive, SortOrder, GroupType) VALUES
(2, 'Item', N'Item / Product Master', 'Stg_Item', 'V_Items', 'usp_Sync_Item', 1, 2, 'Master'),
(3, 'ItemCategory', N'Category Master', 'Stg_ItemCategory', 'V_Masters', 'usp_Sync_ItemCategory', 1, 3, 'Master'),
(4, 'ItemType', N'Item Sub-Category / Format Master', 'Stg_ItemType', 'V_Masters', 'usp_Sync_ItemType', 1, 4, 'Master'),
(5, 'Brand', N'Brand Master', 'Stg_Brand', 'V_Masters', 'usp_Sync_Brand', 1, 5, 'Master'),
(6, 'Unit', N'Unit of Measurement (UOM) Master', 'Stg_Unit', 'V_Masters', 'usp_Sync_Unit', 1, 6, 'Master'),
(7, 'Customer', N'Customer Master', 'Stg_Customer', 'V_Customers', 'usp_Sync_Customer', 1, 7, 'Master'),
(8, 'Supplier', N'Supplier Master', 'Stg_Supplier', 'V_Suppliers', 'usp_Sync_Supplier', 1, 8, 'Master'),
(9, 'Tax', N'Tax / GST Master', 'Stg_Tax', 'V_Masters', 'usp_Sync_Tax', 1, 9, 'Master');

SET IDENTITY_INSERT dbo.UploadType OFF;
GO

-- Columns: Common Transaction (Id = 1)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(1, 'TransactionType*', 'TransactionType', 'NVARCHAR', 1, 50, 0, 'Stock', 1),
(1, 'DocumentNumber', 'DocumentNumber', 'NVARCHAR', 0, 100, 0, 'DOC-1001', 2),
(1, 'TransactionDate', 'TransactionDate', 'DATE', 0, 50, 0, '2026-10-29', 3),
(1, 'ItemCode', 'ItemCode', 'NVARCHAR', 0, 100, 0, 'ITM-001', 4),
(1, 'ItemName*', 'ItemName', 'NVARCHAR', 1, 250, 0, 'Tata Tea Gold 250g', 5),
(1, 'ItemType', 'ItemType', 'NVARCHAR', 0, 150, 0, 'Packed', 6),
(1, 'Category', 'Category', 'NVARCHAR', 0, 150, 0, 'Beverages', 7),
(1, 'Brand', 'Brand', 'NVARCHAR', 0, 150, 0, 'Tata', 8),
(1, 'Unit', 'Unit', 'NVARCHAR', 0, 50, 0, 'pcs', 9),
(1, 'Barcode', 'Barcode', 'NVARCHAR', 0, 100, 0, '8901234567890', 10),
(1, 'Quantity*', 'Quantity', 'DECIMAL', 1, NULL, 0, '50.000', 11),
(1, 'Rate*', 'Rate', 'DECIMAL', 1, NULL, 0, '140.00', 12),
(1, 'Discount', 'Discount', 'DECIMAL', 0, NULL, 0, '0.00', 13),
(1, 'Tax', 'Tax', 'DECIMAL', 0, NULL, 0, '5.00', 14),
(1, 'BatchNumber', 'BatchNumber', 'NVARCHAR', 0, 100, 0, 'B-2026-A', 15),
(1, 'ExpiryDate', 'ExpiryDate', 'DATE', 0, 50, 0, '2027-12-31', 16),
(1, 'CustomerOrSupplier', 'CustomerOrSupplier', 'NVARCHAR', 0, 200, 0, 'Direct Supplier / Walk-in', 17),
(1, 'PaymentMode', 'PaymentMode', 'NVARCHAR', 0, 50, 0, 'Cash', 18),
(1, 'Remarks', 'Remarks', 'NVARCHAR', 0, 500, 0, 'Opening inventory load', 19);

-- Columns: Item / Product Master (Id = 2)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(2, 'ItemCode', 'ItemCode', 'NVARCHAR', 0, 100, 0, 'ITM-TATA-TEA', 1),
(2, 'Product Name*', 'Name', 'NVARCHAR', 1, 250, 1, 'Tata Tea Gold 250g', 2),
(2, 'Category', 'Category', 'NVARCHAR', 0, 150, 0, 'Beverages', 3),
(2, 'Brand', 'Brand', 'NVARCHAR', 0, 150, 0, 'Tata', 4),
(2, 'Sub-Category / Type', 'ItemType', 'NVARCHAR', 0, 150, 0, 'Packed', 5),
(2, 'Unit', 'Unit', 'NVARCHAR', 0, 50, 0, 'pcs', 6),
(2, 'Barcode / SKU', 'Barcode', 'NVARCHAR', 0, 100, 0, '8901234567890', 7),
(2, 'Purchase Price (Rs)', 'PurchasePrice', 'DECIMAL', 0, NULL, 0, '120.00', 8),
(2, 'Selling Price (Rs)*', 'SellingPrice', 'DECIMAL', 1, NULL, 0, '140.00', 9),
(2, 'MRP (Rs)', 'MRP', 'DECIMAL', 0, NULL, 0, '150.00', 10),
(2, 'Minimum Alert Stock', 'MinStock', 'DECIMAL', 0, NULL, 0, '10', 11),
(2, 'Description', 'Description', 'NVARCHAR', 0, 500, 0, 'Premium leaf blend tea', 12);

-- Columns: Category Master (Id = 3)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(3, 'Category Name*', 'Name', 'NVARCHAR', 1, 150, 1, 'Grocery & Provisions', 1),
(3, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'Daily FMCG packaged goods', 2);

-- Columns: Item Sub-Category / Type Master (Id = 4)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(4, 'Sub-Category Name*', 'Name', 'NVARCHAR', 1, 150, 1, 'Packed Spices', 1),
(4, 'Parent Category*', 'CategoryName', 'NVARCHAR', 1, 150, 0, 'Grocery & Provisions', 2),
(4, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'Ground and whole packaged condiments', 3);

-- Columns: Brand Master (Id = 5)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(5, 'Brand Name*', 'Name', 'NVARCHAR', 1, 150, 1, 'Tata Consumer Products', 1),
(5, 'Manufacturer', 'Manufacturer', 'NVARCHAR', 0, 150, 0, 'Tata Group', 2);

-- Columns: Unit Master (Id = 6)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(6, 'Unit Name*', 'Name', 'NVARCHAR', 1, 100, 1, 'Pieces', 1),
(6, 'Unit Code*', 'Code', 'NVARCHAR', 1, 50, 1, 'pcs', 2),
(6, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'Individual packaged units', 3);

-- Columns: Customer Master (Id = 7)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(7, 'Customer Name*', 'Name', 'NVARCHAR', 1, 150, 0, 'Rajesh Kumar Verma', 1),
(7, 'Mobile Number*', 'Mobile', 'NVARCHAR', 1, 15, 1, '9876543210', 2),
(7, 'Village / Area', 'Village', 'NVARCHAR', 0, 100, 0, 'Bishungarh', 3),
(7, 'Full Address', 'Address', 'NVARCHAR', 0, 300, 0, 'Main Market Ward 4', 4),
(7, 'Opening Udhaar (Rs)', 'OpeningBalance', 'DECIMAL', 0, NULL, 0, '500.00', 5),
(7, 'Credit Limit (Rs)', 'CreditLimit', 'DECIMAL', 0, NULL, 0, '10000.00', 6);

-- Columns: Supplier Master (Id = 8)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(8, 'Supplier Name*', 'Name', 'NVARCHAR', 1, 200, 1, 'Gupta FMCG Wholesale Traders', 1),
(8, 'Mobile Number', 'Mobile', 'NVARCHAR', 0, 20, 0, '9812345678', 2),
(8, 'Address', 'Address', 'NVARCHAR', 0, 300, 0, 'Station Road, Mandi Yard', 3),
(8, 'GST Number', 'GSTNumber', 'NVARCHAR', 0, 50, 0, '09ABCDE1234F1Z5', 4),
(8, 'Opening Balance (Rs)', 'OpeningBalance', 'DECIMAL', 0, NULL, 0, '0.00', 5);

-- Columns: Tax / GST Master (Id = 9)
INSERT INTO dbo.UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(9, 'Tax Name*', 'Name', 'NVARCHAR', 1, 100, 1, 'GST 5%', 1),
(9, 'Tax Rate Percent*', 'RatePercent', 'DECIMAL', 1, NULL, 0, '5.00', 2),
(9, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'Standard 5 percent rate', 3);
GO

-- 3. HIGH-PERFORMANCE, TENANT-ISOLATED SYNC PROCEDURES

-- SYNC 1: COMMON TRANSACTION SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Transaction
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Resolve and create missing Masters for Tenant
        -- 1a. Categories
        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'CATEGORY', RTRIM(LTRIM(s.Category)), UPPER(REPLACE(RTRIM(LTRIM(s.Category)), ' ', '_')), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.Stg_Transaction s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND s.Category IS NOT NULL AND LTRIM(RTRIM(s.Category)) <> ''
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Masters m
              WHERE m.TenantId = @TenantId AND m.MasterType = 'CATEGORY'
                AND LOWER(RTRIM(LTRIM(m.MasterName))) = LOWER(RTRIM(LTRIM(s.Category)))
                AND m.IsDeleted = 0
          );

        -- 1b. Brands
        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'BRAND', RTRIM(LTRIM(s.Brand)), UPPER(REPLACE(RTRIM(LTRIM(s.Brand)), ' ', '_')), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.Stg_Transaction s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND s.Brand IS NOT NULL AND LTRIM(RTRIM(s.Brand)) <> ''
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Masters m
              WHERE m.TenantId = @TenantId AND m.MasterType = 'BRAND'
                AND LOWER(RTRIM(LTRIM(m.MasterName))) = LOWER(RTRIM(LTRIM(s.Brand)))
                AND m.IsDeleted = 0
          );

        -- 1c. Units
        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'UNIT', RTRIM(LTRIM(s.Unit)), LOWER(RTRIM(LTRIM(s.Unit))), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.Stg_Transaction s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND s.Unit IS NOT NULL AND LTRIM(RTRIM(s.Unit)) <> ''
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Masters m
              WHERE m.TenantId = @TenantId AND m.MasterType = 'UNIT'
                AND (LOWER(RTRIM(LTRIM(m.MasterName))) = LOWER(RTRIM(LTRIM(s.Unit))) OR LOWER(RTRIM(LTRIM(m.MasterCode))) = LOWER(RTRIM(LTRIM(s.Unit))))
                AND m.IsDeleted = 0
          );

        -- 2. Resolve or create Items in V_Items
        INSERT INTO dbo.V_Items (TenantId, ItemCode, Name, Category, Unit, Format, CategoryId, BrandId, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
        SELECT DISTINCT
            @TenantId,
            COALESCE(NULLIF(RTRIM(LTRIM(s.ItemCode)), ''), 'ITM-' + SUBSTRING(CONVERT(VARCHAR(40), NEWID()), 1, 8)),
            RTRIM(LTRIM(s.ItemName)),
            s.Category,
            ISNULL(NULLIF(RTRIM(LTRIM(s.Unit)), ''), 'pcs'),
            ISNULL(NULLIF(RTRIM(LTRIM(s.ItemType)), ''), 'Packed'),
            cat.ID,
            brd.ID,
            1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1'
        FROM dbo.Stg_Transaction s
        LEFT JOIN dbo.V_Masters cat ON cat.TenantId = @TenantId AND cat.MasterType = 'CATEGORY' AND LOWER(RTRIM(LTRIM(cat.MasterName))) = LOWER(RTRIM(LTRIM(s.Category))) AND cat.IsDeleted = 0
        LEFT JOIN dbo.V_Masters brd ON brd.TenantId = @TenantId AND brd.MasterType = 'BRAND' AND LOWER(RTRIM(LTRIM(brd.MasterName))) = LOWER(RTRIM(LTRIM(s.Brand))) AND brd.IsDeleted = 0
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Items i
              WHERE i.TenantId = @TenantId
                AND LOWER(RTRIM(LTRIM(i.Name))) = LOWER(RTRIM(LTRIM(s.ItemName)))
                AND i.IsDeleted = 0
          );

        -- 3. Process Stock Adjustments / Opening Stock / Purchases
        MERGE INTO dbo.V_Stock AS target
        USING (
            SELECT
                s.RowNo,
                i.ID AS ItemId,
                i.ItemCode AS ProductCode,
                i.Name AS ItemName,
                s.Category,
                s.Brand,
                ISNULL(NULLIF(s.Unit, ''), 'pcs') AS Unit,
                s.Barcode,
                TRY_CAST(s.Quantity AS DECIMAL(18,3)) AS Qty,
                TRY_CAST(s.Rate AS DECIMAL(18,2)) AS PriceRate,
                TRY_CAST(s.Tax AS DECIMAL(5,2)) AS TaxPercent,
                s.BatchNumber,
                TRY_CAST(s.ExpiryDate AS DATETIME2) AS ExpDate,
                LOWER(RTRIM(LTRIM(s.TransactionType))) AS TxType
            FROM dbo.Stg_Transaction s
            INNER JOIN dbo.V_Items i ON i.TenantId = @TenantId AND LOWER(RTRIM(LTRIM(i.Name))) = LOWER(RTRIM(LTRIM(s.ItemName))) AND i.IsDeleted = 0
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = @TenantId AND target.ItemId = source.ItemId AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.CurrentStock = CASE
                    WHEN source.TxType IN ('stock', 'openingstock') THEN source.Qty
                    WHEN source.TxType IN ('purchase', 'salereturn') THEN target.CurrentStock + source.Qty
                    WHEN source.TxType IN ('sale', 'purchasereturn') THEN target.CurrentStock - source.Qty
                    ELSE target.CurrentStock
                END,
                target.PurchasePrice = CASE WHEN source.TxType IN ('stock', 'openingstock', 'purchase') THEN ISNULL(source.PriceRate, target.PurchasePrice) ELSE target.PurchasePrice END,
                target.SellingPrice = CASE WHEN source.TxType = 'sale' THEN ISNULL(source.PriceRate, target.SellingPrice) ELSE target.SellingPrice END,
                target.Barcode = COALESCE(NULLIF(source.Barcode, ''), target.Barcode),
                target.BatchNumber = COALESCE(NULLIF(source.BatchNumber, ''), target.BatchNumber),
                target.ExpiryDate = COALESCE(source.ExpDate, target.ExpiryDate),
                target.GSTPercent = ISNULL(source.TaxPercent, target.GSTPercent),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, ItemId, ProductCode, Name, Category, Brand, Unit, Barcode, PurchasePrice, SellingPrice, MRP, GSTPercent, OpeningStock, MinimumStock, CurrentStock, BatchNumber, ExpiryDate, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
            VALUES (
                @TenantId,
                source.ItemId,
                source.ProductCode,
                source.ItemName,
                source.Category,
                source.Brand,
                source.Unit,
                source.Barcode,
                CASE WHEN source.TxType IN ('stock', 'openingstock', 'purchase') THEN source.PriceRate ELSE source.PriceRate * 0.8 END,
                CASE WHEN source.TxType = 'sale' THEN source.PriceRate ELSE source.PriceRate * 1.2 END,
                source.PriceRate * 1.25,
                ISNULL(source.TaxPercent, 0),
                source.Qty,
                5,
                CASE WHEN source.TxType IN ('sale', 'purchasereturn') THEN -source.Qty ELSE source.Qty END,
                source.BatchNumber,
                source.ExpDate,
                1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1'
            );

        -- 4. Mark rows as Synced
        UPDATE dbo.Stg_Transaction
        SET RowStatus = 'Synced', ErrorMessage = NULL
        WHERE BatchId = @BatchId AND RowStatus = 'Valid';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SYNC 2: DEDICATED ITEM MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Item
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    MERGE INTO dbo.V_Items AS target
    USING (
        SELECT
            s.RowNo,
            COALESCE(NULLIF(RTRIM(LTRIM(s.ItemCode)), ''), 'ITM-' + SUBSTRING(CONVERT(VARCHAR(40), NEWID()), 1, 8)) AS ItemCode,
            RTRIM(LTRIM(s.Name)) AS Name,
            s.Category,
            s.Brand,
            ISNULL(NULLIF(RTRIM(LTRIM(s.Unit)), ''), 'pcs') AS Unit,
            ISNULL(NULLIF(RTRIM(LTRIM(s.ItemType)), ''), 'Packed') AS Format,
            s.Description,
            cat.ID AS CategoryId,
            brd.ID AS BrandId
        FROM dbo.Stg_Item s
        LEFT JOIN dbo.V_Masters cat ON cat.TenantId = @TenantId AND cat.MasterType = 'CATEGORY' AND LOWER(RTRIM(LTRIM(cat.MasterName))) = LOWER(RTRIM(LTRIM(s.Category))) AND cat.IsDeleted = 0
        LEFT JOIN dbo.V_Masters brd ON brd.TenantId = @TenantId AND brd.MasterType = 'BRAND' AND LOWER(RTRIM(LTRIM(brd.MasterName))) = LOWER(RTRIM(LTRIM(s.Brand))) AND brd.IsDeleted = 0
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
    ) AS source
    ON (target.TenantId = @TenantId AND LOWER(RTRIM(LTRIM(target.Name))) = LOWER(RTRIM(LTRIM(source.Name))) AND target.IsDeleted = 0)
    WHEN MATCHED THEN
        UPDATE SET
            target.Category = ISNULL(source.Category, target.Category),
            target.Unit = source.Unit,
            target.Format = source.Format,
            target.CategoryId = ISNULL(source.CategoryId, target.CategoryId),
            target.BrandId = ISNULL(source.BrandId, target.BrandId),
            target.Description = ISNULL(source.Description, target.Description),
            target.ModifiedDate = SYSUTCDATETIME(),
            target.ModifiedBy = @UserId
    WHEN NOT MATCHED THEN
        INSERT (TenantId, ItemCode, Name, Category, Unit, Format, CategoryId, BrandId, Description, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
        VALUES (@TenantId, source.ItemCode, source.Name, source.Category, source.Unit, source.Format, source.CategoryId, source.BrandId, source.Description, 1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1');

    -- Also ensure stock records exist for items
    MERGE INTO dbo.V_Stock AS target
    USING (
        SELECT
            i.ID AS ItemId,
            i.ItemCode AS ProductCode,
            i.Name,
            i.Category,
            s.Brand,
            i.Unit,
            s.Barcode,
            ISNULL(TRY_CAST(s.PurchasePrice AS DECIMAL(18,2)), 0) AS PurchasePrice,
            ISNULL(TRY_CAST(s.SellingPrice AS DECIMAL(18,2)), 0) AS SellingPrice,
            ISNULL(TRY_CAST(s.MRP AS DECIMAL(18,2)), 0) AS MRP,
            ISNULL(TRY_CAST(s.MinStock AS DECIMAL(18,3)), 5) AS MinStock
        FROM dbo.Stg_Item s
        INNER JOIN dbo.V_Items i ON i.TenantId = @TenantId AND LOWER(RTRIM(LTRIM(i.Name))) = LOWER(RTRIM(LTRIM(s.Name))) AND i.IsDeleted = 0
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
    ) AS source
    ON (target.TenantId = @TenantId AND target.ItemId = source.ItemId AND target.IsDeleted = 0)
    WHEN MATCHED THEN
        UPDATE SET
            target.PurchasePrice = CASE WHEN source.PurchasePrice > 0 THEN source.PurchasePrice ELSE target.PurchasePrice END,
            target.SellingPrice = CASE WHEN source.SellingPrice > 0 THEN source.SellingPrice ELSE target.SellingPrice END,
            target.MRP = CASE WHEN source.MRP > 0 THEN source.MRP ELSE target.MRP END,
            target.MinimumStock = source.MinStock,
            target.Barcode = COALESCE(NULLIF(source.Barcode, ''), target.Barcode),
            target.ModifiedDate = SYSUTCDATETIME(),
            target.ModifiedBy = @UserId
    WHEN NOT MATCHED THEN
        INSERT (TenantId, ItemId, ProductCode, Name, Category, Brand, Unit, Barcode, PurchasePrice, SellingPrice, MRP, GSTPercent, OpeningStock, MinimumStock, CurrentStock, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
        VALUES (@TenantId, source.ItemId, source.ProductCode, source.Name, source.Category, source.Brand, source.Unit, source.Barcode, source.PurchasePrice, source.SellingPrice, source.MRP, 0, 0, source.MinStock, 0, 1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1');

    UPDATE dbo.Stg_Item
    SET RowStatus = 'Synced', ErrorMessage = NULL
    WHERE BatchId = @BatchId AND RowStatus = 'Valid';

    COMMIT TRANSACTION;
END;
GO

-- SYNC 3: DEDICATED UNIT MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Unit
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    MERGE INTO dbo.V_Masters AS target
    USING (
        SELECT
            RTRIM(LTRIM(s.Name)) AS Name,
            LOWER(RTRIM(LTRIM(s.Code))) AS Code,
            s.Description
        FROM dbo.Stg_Unit s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
    ) AS source
    ON (target.TenantId = @TenantId AND target.MasterType = 'UNIT' AND (LOWER(RTRIM(LTRIM(target.MasterName))) = LOWER(source.Name) OR LOWER(RTRIM(LTRIM(target.MasterCode))) = LOWER(source.Code)) AND target.IsDeleted = 0)
    WHEN MATCHED THEN
        UPDATE SET
            target.MasterName = source.Name,
            target.MasterCode = source.Code,
            target.Description = ISNULL(source.Description, target.Description),
            target.ModifiedDate = SYSUTCDATETIME(),
            target.ModifiedBy = @UserId
    WHEN NOT MATCHED THEN
        INSERT (TenantId, MasterType, MasterName, MasterCode, Description, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        VALUES (@TenantId, 'UNIT', source.Name, source.Code, source.Description, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0);

    UPDATE dbo.Stg_Unit
    SET RowStatus = 'Synced', ErrorMessage = NULL
    WHERE BatchId = @BatchId AND RowStatus = 'Valid';

    COMMIT TRANSACTION;
END;
GO

-- SYNC 4: DEDICATED SUPPLIER MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Supplier
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    MERGE INTO dbo.V_Suppliers AS target
    USING (
        SELECT
            RTRIM(LTRIM(s.Name)) AS Name,
            s.Mobile,
            s.Address,
            s.GSTNumber,
            ISNULL(TRY_CAST(s.OpeningBalance AS DECIMAL(18,2)), 0) AS OpeningBalance
        FROM dbo.Stg_Supplier s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
    ) AS source
    ON (target.TenantId = @TenantId AND LOWER(RTRIM(LTRIM(target.Name))) = LOWER(source.Name) AND target.IsDeleted = 0)
    WHEN MATCHED THEN
        UPDATE SET
            target.Mobile = COALESCE(NULLIF(source.Mobile, ''), target.Mobile),
            target.Address = COALESCE(NULLIF(source.Address, ''), target.Address),
            target.GSTNumber = COALESCE(NULLIF(source.GSTNumber, ''), target.GSTNumber),
            target.ModifiedDate = SYSUTCDATETIME(),
            target.ModifiedBy = @UserId
    WHEN NOT MATCHED THEN
        INSERT (TenantId, Name, Mobile, Address, GSTNumber, OpeningBalance, CurrentBalance, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
        VALUES (@TenantId, source.Name, source.Mobile, source.Address, source.GSTNumber, source.OpeningBalance, source.OpeningBalance, 1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1');

    UPDATE dbo.Stg_Supplier
    SET RowStatus = 'Synced', ErrorMessage = NULL
    WHERE BatchId = @BatchId AND RowStatus = 'Valid';

    COMMIT TRANSACTION;
END;
GO

-- SYNC 5: DEDICATED TAX MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Tax
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    MERGE INTO dbo.V_Masters AS target
    USING (
        SELECT
            RTRIM(LTRIM(s.Name)) AS Name,
            RTRIM(LTRIM(s.RatePercent)) AS Code,
            s.Description
        FROM dbo.Stg_Tax s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
    ) AS source
    ON (target.TenantId = @TenantId AND target.MasterType = 'TAX' AND LOWER(RTRIM(LTRIM(target.MasterName))) = LOWER(source.Name) AND target.IsDeleted = 0)
    WHEN MATCHED THEN
        UPDATE SET
            target.MasterCode = source.Code,
            target.Description = ISNULL(source.Description, target.Description),
            target.ModifiedDate = SYSUTCDATETIME(),
            target.ModifiedBy = @UserId
    WHEN NOT MATCHED THEN
        INSERT (TenantId, MasterType, MasterName, MasterCode, Description, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        VALUES (@TenantId, 'TAX', source.Name, source.Code, source.Description, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0);

    UPDATE dbo.Stg_Tax
    SET RowStatus = 'Synced', ErrorMessage = NULL
    WHERE BatchId = @BatchId AND RowStatus = 'Valid';

    COMMIT TRANSACTION;
END;
GO
