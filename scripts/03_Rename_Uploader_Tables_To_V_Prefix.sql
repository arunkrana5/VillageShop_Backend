-- ============================================================================
-- SCRIPT: STANDARDIZE ALL VILLAGESHOP UPLOADER TABLES WITH 'V_' PREFIX
-- Tables: V_UploadType, V_UploadColumn, V_UploadBatch, V_Stg_*
-- Procedures: usp_Sync_* pointing to V_Stg_*
-- Backward-compatibility views/synonyms
-- ============================================================================

USE [dev.inhouse];
GO

-- 1. Create V_UploadType
IF OBJECT_ID('dbo.V_UploadType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_UploadType (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code VARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        StageTable VARCHAR(100) NOT NULL,
        MainTable VARCHAR(100) NOT NULL,
        SyncProcName VARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        SortOrder INT NOT NULL DEFAULT 0,
        GroupType VARCHAR(20) NOT NULL DEFAULT 'Master'
    );
END
GO

-- 2. Create V_UploadColumn
IF OBJECT_ID('dbo.V_UploadColumn', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_UploadColumn (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UploadTypeId INT NOT NULL FOREIGN KEY REFERENCES dbo.V_UploadType(Id) ON DELETE CASCADE,
        ExcelHeader NVARCHAR(100) NOT NULL,
        DbColumn VARCHAR(100) NOT NULL,
        DataType VARCHAR(20) NOT NULL,
        IsRequired BIT NOT NULL DEFAULT 0,
        MaxLength INT NULL,
        IsKey BIT NOT NULL DEFAULT 0,
        LookupTable VARCHAR(100) NULL,
        LookupColumn VARCHAR(100) NULL,
        DefaultValue NVARCHAR(200) NULL,
        SampleValue NVARCHAR(200) NULL,
        SortOrder INT NOT NULL DEFAULT 0
    );
    CREATE NONCLUSTERED INDEX IX_V_UploadColumn_Type ON dbo.V_UploadColumn(UploadTypeId, SortOrder);
END
GO

-- 3. Create V_UploadBatch
IF OBJECT_ID('dbo.V_UploadBatch', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_UploadBatch (
        BatchId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TenantId BIGINT NOT NULL DEFAULT 1,
        UploadTypeId INT NOT NULL FOREIGN KEY REFERENCES dbo.V_UploadType(Id),
        FileName NVARCHAR(255) NOT NULL,
        TotalRows INT NOT NULL DEFAULT 0,
        SuccessRows INT NOT NULL DEFAULT 0,
        ErrorRows INT NOT NULL DEFAULT 0,
        Status VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
        ErrorMessage NVARCHAR(MAX) NULL,
        UploadedBy BIGINT NOT NULL DEFAULT 0,
        CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CompletedOn DATETIME2 NULL
    );
    CREATE NONCLUSTERED INDEX IX_V_UploadBatch_Tenant ON dbo.V_UploadBatch(TenantId, CreatedOn DESC);
END
GO

-- 4. Create V_ Staging Tables

-- V_Stg_Transaction (Common Transaction Template)
IF OBJECT_ID('dbo.V_Stg_Transaction', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_Transaction (
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
    CREATE NONCLUSTERED INDEX IX_V_Stg_Transaction_Batch ON dbo.V_Stg_Transaction(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_Item (Item / Product Master)
IF OBJECT_ID('dbo.V_Stg_Item', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_Item (
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
    CREATE NONCLUSTERED INDEX IX_V_Stg_Item_Batch ON dbo.V_Stg_Item(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_ItemCategory
IF OBJECT_ID('dbo.V_Stg_ItemCategory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_ItemCategory (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        BatchId UNIQUEIDENTIFIER NOT NULL,
        RowNo INT NOT NULL,
        TenantId BIGINT NOT NULL DEFAULT 1,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(300) NULL,
        RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
        ErrorMessage NVARCHAR(1000) NULL
    );
    CREATE NONCLUSTERED INDEX IX_V_Stg_ItemCategory_Batch ON dbo.V_Stg_ItemCategory(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_ItemType
IF OBJECT_ID('dbo.V_Stg_ItemType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_ItemType (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        BatchId UNIQUEIDENTIFIER NOT NULL,
        RowNo INT NOT NULL,
        TenantId BIGINT NOT NULL DEFAULT 1,
        Name NVARCHAR(150) NOT NULL,
        CategoryName NVARCHAR(150) NOT NULL,
        Description NVARCHAR(300) NULL,
        RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
        ErrorMessage NVARCHAR(1000) NULL
    );
    CREATE NONCLUSTERED INDEX IX_V_Stg_ItemType_Batch ON dbo.V_Stg_ItemType(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_Brand
IF OBJECT_ID('dbo.V_Stg_Brand', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_Brand (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        BatchId UNIQUEIDENTIFIER NOT NULL,
        RowNo INT NOT NULL,
        TenantId BIGINT NOT NULL DEFAULT 1,
        Name NVARCHAR(150) NOT NULL,
        Manufacturer NVARCHAR(150) NULL,
        RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
        ErrorMessage NVARCHAR(1000) NULL
    );
    CREATE NONCLUSTERED INDEX IX_V_Stg_Brand_Batch ON dbo.V_Stg_Brand(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_Unit
IF OBJECT_ID('dbo.V_Stg_Unit', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_Unit (
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
    CREATE NONCLUSTERED INDEX IX_V_Stg_Unit_Batch ON dbo.V_Stg_Unit(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_Customer
IF OBJECT_ID('dbo.V_Stg_Customer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_Customer (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        BatchId UNIQUEIDENTIFIER NOT NULL,
        RowNo INT NOT NULL,
        TenantId BIGINT NOT NULL DEFAULT 1,
        Name NVARCHAR(150) NOT NULL,
        Mobile NVARCHAR(15) NOT NULL,
        Village NVARCHAR(100) NULL,
        Address NVARCHAR(300) NULL,
        OpeningBalance NVARCHAR(50) NULL,
        CreditLimit NVARCHAR(50) NULL,
        RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
        ErrorMessage NVARCHAR(1000) NULL
    );
    CREATE NONCLUSTERED INDEX IX_V_Stg_Customer_Batch ON dbo.V_Stg_Customer(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_Supplier
IF OBJECT_ID('dbo.V_Stg_Supplier', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_Supplier (
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
    CREATE NONCLUSTERED INDEX IX_V_Stg_Supplier_Batch ON dbo.V_Stg_Supplier(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_Tax
IF OBJECT_ID('dbo.V_Stg_Tax', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_Tax (
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
    CREATE NONCLUSTERED INDEX IX_V_Stg_Tax_Batch ON dbo.V_Stg_Tax(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- V_Stg_Stock
IF OBJECT_ID('dbo.V_Stg_Stock', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.V_Stg_Stock (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        BatchId UNIQUEIDENTIFIER NOT NULL,
        RowNo INT NOT NULL,
        TenantId BIGINT NOT NULL DEFAULT 1,
        ProductCode NVARCHAR(100) NULL,
        Name NVARCHAR(250) NOT NULL,
        Category NVARCHAR(150) NULL,
        Brand NVARCHAR(150) NULL,
        Unit NVARCHAR(50) NULL,
        Barcode NVARCHAR(100) NULL,
        PurchasePrice NVARCHAR(50) NULL,
        SellingPrice NVARCHAR(50) NOT NULL,
        MRP NVARCHAR(50) NULL,
        OpeningStock NVARCHAR(50) NULL,
        MinimumStock NVARCHAR(50) NULL,
        CurrentStock NVARCHAR(50) NOT NULL,
        BatchNumber NVARCHAR(100) NULL,
        ExpiryDate NVARCHAR(50) NULL,
        RowStatus VARCHAR(20) NOT NULL DEFAULT 'Uploaded',
        ErrorMessage NVARCHAR(1000) NULL
    );
    CREATE NONCLUSTERED INDEX IX_V_Stg_Stock_Batch ON dbo.V_Stg_Stock(BatchId, RowStatus) INCLUDE (RowNo);
END
GO

-- 5. POPULATE / RE-SEED V_UploadType and V_UploadColumn
DELETE FROM dbo.V_UploadColumn;
DELETE FROM dbo.V_UploadType;
GO

SET IDENTITY_INSERT dbo.V_UploadType ON;

-- 1. COMMON TRANSACTION TYPE
INSERT INTO dbo.V_UploadType (Id, Code, Name, StageTable, MainTable, SyncProcName, IsActive, SortOrder, GroupType)
VALUES (1, 'Transaction', N'Common Transaction (Stock, Sale, Purchase, Returns)', 'V_Stg_Transaction', 'V_Stock', 'usp_Sync_Transaction', 1, 1, 'Transaction');

-- 2. DEDICATED MASTER TYPES
INSERT INTO dbo.V_UploadType (Id, Code, Name, StageTable, MainTable, SyncProcName, IsActive, SortOrder, GroupType) VALUES
(2, 'Item', N'Item / Product Master', 'V_Stg_Item', 'V_Items', 'usp_Sync_Item', 1, 2, 'Master'),
(3, 'ItemCategory', N'Category Master', 'V_Stg_ItemCategory', 'V_Masters', 'usp_Sync_ItemCategory', 1, 3, 'Master'),
(4, 'ItemType', N'Item Sub-Category / Format Master', 'V_Stg_ItemType', 'V_Masters', 'usp_Sync_ItemType', 1, 4, 'Master'),
(5, 'Brand', N'Brand Master', 'V_Stg_Brand', 'V_Masters', 'usp_Sync_Brand', 1, 5, 'Master'),
(6, 'Unit', N'Unit of Measurement (UOM) Master', 'V_Stg_Unit', 'V_Masters', 'usp_Sync_Unit', 1, 6, 'Master'),
(7, 'Customer', N'Customer Master', 'V_Stg_Customer', 'V_Customers', 'usp_Sync_Customer', 1, 7, 'Master'),
(8, 'Supplier', N'Supplier Master', 'V_Stg_Supplier', 'V_Suppliers', 'usp_Sync_Supplier', 1, 8, 'Master'),
(9, 'Tax', N'Tax / GST Master', 'V_Stg_Tax', 'V_Masters', 'usp_Sync_Tax', 1, 9, 'Master');

SET IDENTITY_INSERT dbo.V_UploadType OFF;
GO

-- Columns: Common Transaction (Id = 1)
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
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
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
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
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(3, 'Category Name*', 'Name', 'NVARCHAR', 1, 150, 1, 'Grocery & Provisions', 1),
(3, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'Daily FMCG packaged goods', 2);

-- Columns: Item Sub-Category / Type Master (Id = 4)
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(4, 'Sub-Category Name*', 'Name', 'NVARCHAR', 1, 150, 1, 'Packed Spices', 1),
(4, 'Parent Category*', 'CategoryName', 'NVARCHAR', 1, 150, 0, 'Grocery & Provisions', 2),
(4, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'Ground and whole packaged condiments', 3);

-- Columns: Brand Master (Id = 5)
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(5, 'Brand Name*', 'Name', 'NVARCHAR', 1, 150, 1, 'Tata Consumer Products', 1),
(5, 'Manufacturer', 'Manufacturer', 'NVARCHAR', 0, 150, 0, 'Tata Group', 2);

-- Columns: Unit Master (Id = 6)
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(6, 'Unit Name*', 'Name', 'NVARCHAR', 1, 100, 1, 'Pieces', 1),
(6, 'Unit Code*', 'Code', 'NVARCHAR', 1, 50, 1, 'pcs', 2),
(6, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'Individual packaged units', 3);

-- Columns: Customer Master (Id = 7)
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(7, 'Customer Name*', 'Name', 'NVARCHAR', 1, 150, 0, 'Rajesh Kumar Verma', 1),
(7, 'Mobile Number*', 'Mobile', 'NVARCHAR', 1, 15, 1, '9876543210', 2),
(7, 'Village / Area', 'Village', 'NVARCHAR', 0, 100, 0, 'Bishungarh', 3),
(7, 'Full Address', 'Address', 'NVARCHAR', 0, 300, 0, 'Main Market Ward 4', 4),
(7, 'Opening Udhaar (Rs)', 'OpeningBalance', 'DECIMAL', 0, NULL, 0, '500.00', 5),
(7, 'Credit Limit (Rs)', 'CreditLimit', 'DECIMAL', 0, NULL, 0, '10000.00', 6);

-- Columns: Supplier Master (Id = 8)
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(8, 'Supplier Name*', 'Name', 'NVARCHAR', 1, 200, 1, 'Gupta FMCG Wholesale Traders', 1),
(8, 'Mobile Number', 'Mobile', 'NVARCHAR', 0, 20, 0, '9812345678', 2),
(8, 'Address', 'Address', 'NVARCHAR', 0, 300, 0, 'Station Road, Mandi Yard', 3),
(8, 'GST Number', 'GSTNumber', 'NVARCHAR', 0, 50, 0, '09ABCDE1234F1Z5', 4),
(8, 'Opening Balance (Rs)', 'OpeningBalance', 'DECIMAL', 0, NULL, 0, '0.00', 5);

-- Columns: Tax / GST Master (Id = 9)
INSERT INTO dbo.V_UploadColumn (UploadTypeId, ExcelHeader, DbColumn, DataType, IsRequired, MaxLength, IsKey, SampleValue, SortOrder) VALUES
(9, 'Tax Name*', 'Name', 'NVARCHAR', 1, 100, 1, 'GST 5%', 1),
(9, 'Tax Rate Percent*', 'RatePercent', 'DECIMAL', 1, NULL, 0, '5.00', 2),
(9, 'Description', 'Description', 'NVARCHAR', 0, 300, 0, 'Standard 5 percent rate', 3);
GO

-- 6. SYNC STORED PROCEDURES (USING V_Stg_* TABLES)

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
        -- Categories
        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'CATEGORY', RTRIM(LTRIM(s.Category)), UPPER(REPLACE(RTRIM(LTRIM(s.Category)), ' ', '_')), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.V_Stg_Transaction s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND s.Category IS NOT NULL AND LTRIM(RTRIM(s.Category)) <> ''
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Masters m
              WHERE m.TenantId = @TenantId AND m.MasterType = 'CATEGORY'
                AND LOWER(RTRIM(LTRIM(m.MasterName))) = LOWER(RTRIM(LTRIM(s.Category)))
                AND m.IsDeleted = 0
          );

        -- Brands
        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'BRAND', RTRIM(LTRIM(s.Brand)), UPPER(REPLACE(RTRIM(LTRIM(s.Brand)), ' ', '_')), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.V_Stg_Transaction s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND s.Brand IS NOT NULL AND LTRIM(RTRIM(s.Brand)) <> ''
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Masters m
              WHERE m.TenantId = @TenantId AND m.MasterType = 'BRAND'
                AND LOWER(RTRIM(LTRIM(m.MasterName))) = LOWER(RTRIM(LTRIM(s.Brand)))
                AND m.IsDeleted = 0
          );

        -- Units
        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'UNIT', RTRIM(LTRIM(s.Unit)), LOWER(RTRIM(LTRIM(s.Unit))), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.V_Stg_Transaction s
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
        FROM dbo.V_Stg_Transaction s
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
            FROM dbo.V_Stg_Transaction s
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
        UPDATE dbo.V_Stg_Transaction
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

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Auto-provision Category, Brand, Unit masters
        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'CATEGORY', RTRIM(LTRIM(s.Category)), UPPER(REPLACE(RTRIM(LTRIM(s.Category)), ' ', '_')), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.V_Stg_Item s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND s.Category IS NOT NULL AND LTRIM(RTRIM(s.Category)) <> ''
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Masters m
              WHERE m.TenantId = @TenantId AND m.MasterType = 'CATEGORY'
                AND LOWER(RTRIM(LTRIM(m.MasterName))) = LOWER(RTRIM(LTRIM(s.Category)))
                AND m.IsDeleted = 0
          );

        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'BRAND', RTRIM(LTRIM(s.Brand)), UPPER(REPLACE(RTRIM(LTRIM(s.Brand)), ' ', '_')), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.V_Stg_Item s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND s.Brand IS NOT NULL AND LTRIM(RTRIM(s.Brand)) <> ''
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Masters m
              WHERE m.TenantId = @TenantId AND m.MasterType = 'BRAND'
                AND LOWER(RTRIM(LTRIM(m.MasterName))) = LOWER(RTRIM(LTRIM(s.Brand)))
                AND m.IsDeleted = 0
          );

        -- Merge into V_Items
        MERGE INTO dbo.V_Items AS target
        USING (
            SELECT
                s.RowNo,
                COALESCE(NULLIF(RTRIM(LTRIM(s.ItemCode)), ''), 'ITM-' + SUBSTRING(CONVERT(VARCHAR(40), NEWID()), 1, 8)) AS ItemCode,
                RTRIM(LTRIM(s.Name)) AS Name,
                s.Category,
                s.ItemType,
                ISNULL(NULLIF(s.Unit, ''), 'pcs') AS Unit,
                cat.ID AS CategoryId,
                brd.ID AS BrandId
            FROM dbo.V_Stg_Item s
            LEFT JOIN dbo.V_Masters cat ON cat.TenantId = @TenantId AND cat.MasterType = 'CATEGORY' AND LOWER(RTRIM(LTRIM(cat.MasterName))) = LOWER(RTRIM(LTRIM(s.Category))) AND cat.IsDeleted = 0
            LEFT JOIN dbo.V_Masters brd ON brd.TenantId = @TenantId AND brd.MasterType = 'BRAND' AND LOWER(RTRIM(LTRIM(brd.MasterName))) = LOWER(RTRIM(LTRIM(s.Brand))) AND brd.IsDeleted = 0
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = @TenantId AND LOWER(RTRIM(LTRIM(target.Name))) = LOWER(RTRIM(LTRIM(source.Name))) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.Category = COALESCE(source.Category, target.Category),
                target.Unit = COALESCE(source.Unit, target.Unit),
                target.CategoryId = COALESCE(source.CategoryId, target.CategoryId),
                target.BrandId = COALESCE(source.BrandId, target.BrandId),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, ItemCode, Name, Category, Unit, Format, CategoryId, BrandId, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
            VALUES (@TenantId, source.ItemCode, source.Name, source.Category, source.Unit, ISNULL(source.ItemType, 'Packed'), source.CategoryId, source.BrandId, 1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1');

        -- Also ensure record in V_Stock
        MERGE INTO dbo.V_Stock AS target
        USING (
            SELECT
                i.ID AS ItemId,
                i.ItemCode AS ProductCode,
                i.Name AS ItemName,
                s.Category,
                s.Brand,
                s.Unit,
                s.Barcode,
                TRY_CAST(s.PurchasePrice AS DECIMAL(18,2)) AS PurchasePrice,
                TRY_CAST(s.SellingPrice AS DECIMAL(18,2)) AS SellingPrice,
                TRY_CAST(s.MRP AS DECIMAL(18,2)) AS MRP,
                TRY_CAST(s.MinStock AS DECIMAL(18,3)) AS MinStock
            FROM dbo.V_Stg_Item s
            INNER JOIN dbo.V_Items i ON i.TenantId = @TenantId AND LOWER(RTRIM(LTRIM(i.Name))) = LOWER(RTRIM(LTRIM(s.Name))) AND i.IsDeleted = 0
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = @TenantId AND target.ItemId = source.ItemId AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.PurchasePrice = COALESCE(source.PurchasePrice, target.PurchasePrice),
                target.SellingPrice = COALESCE(source.SellingPrice, target.SellingPrice),
                target.MRP = COALESCE(source.MRP, target.MRP),
                target.MinimumStock = COALESCE(source.MinStock, target.MinimumStock),
                target.Barcode = COALESCE(source.Barcode, target.Barcode),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, ItemId, ProductCode, Name, Category, Brand, Unit, Barcode, PurchasePrice, SellingPrice, MRP, GSTPercent, OpeningStock, MinimumStock, CurrentStock, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
            VALUES (
                @TenantId,
                source.ItemId,
                source.ProductCode,
                source.ItemName,
                source.Category,
                source.Brand,
                ISNULL(source.Unit, 'pcs'),
                source.Barcode,
                ISNULL(source.PurchasePrice, 0),
                ISNULL(source.SellingPrice, 0),
                ISNULL(source.MRP, 0),
                0, 0, ISNULL(source.MinStock, 5), 0, 1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1'
            );

        UPDATE dbo.V_Stg_Item
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

-- SYNC 3: CATEGORY MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_ItemCategory
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        MERGE INTO dbo.V_Masters AS target
        USING (
            SELECT DISTINCT
                @TenantId AS TenantId,
                'CATEGORY' AS MasterType,
                RTRIM(LTRIM(s.Name)) AS MasterName,
                UPPER(REPLACE(RTRIM(LTRIM(s.Name)), ' ', '_')) AS MasterCode,
                s.Description
            FROM dbo.V_Stg_ItemCategory s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = source.TenantId AND target.MasterType = source.MasterType AND LOWER(RTRIM(LTRIM(target.MasterName))) = LOWER(source.MasterName) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.Description = COALESCE(source.Description, target.Description),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, MasterType, MasterName, MasterCode, Description, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
            VALUES (source.TenantId, source.MasterType, source.MasterName, source.MasterCode, source.Description, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0);

        UPDATE dbo.V_Stg_ItemCategory
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

-- SYNC 4: SUB-CATEGORY / ITEM TYPE MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_ItemType
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Ensure parent category exists
        INSERT INTO dbo.V_Masters (TenantId, MasterType, MasterName, MasterCode, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
        SELECT DISTINCT @TenantId, 'CATEGORY', RTRIM(LTRIM(s.CategoryName)), UPPER(REPLACE(RTRIM(LTRIM(s.CategoryName)), ' ', '_')), 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0
        FROM dbo.V_Stg_ItemType s
        WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
          AND NOT EXISTS (
              SELECT 1 FROM dbo.V_Masters m
              WHERE m.TenantId = @TenantId AND m.MasterType = 'CATEGORY'
                AND LOWER(RTRIM(LTRIM(m.MasterName))) = LOWER(RTRIM(LTRIM(s.CategoryName)))
                AND m.IsDeleted = 0
          );

        -- Merge Item Type
        MERGE INTO dbo.V_Masters AS target
        USING (
            SELECT DISTINCT
                @TenantId AS TenantId,
                'ITEM_TYPE' AS MasterType,
                RTRIM(LTRIM(s.Name)) AS MasterName,
                UPPER(REPLACE(RTRIM(LTRIM(s.Name)), ' ', '_')) AS MasterCode,
                s.Description,
                cat.ID AS ParentId
            FROM dbo.V_Stg_ItemType s
            LEFT JOIN dbo.V_Masters cat ON cat.TenantId = @TenantId AND cat.MasterType = 'CATEGORY' AND LOWER(RTRIM(LTRIM(cat.MasterName))) = LOWER(RTRIM(LTRIM(s.CategoryName))) AND cat.IsDeleted = 0
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = source.TenantId AND target.MasterType = source.MasterType AND LOWER(RTRIM(LTRIM(target.MasterName))) = LOWER(source.MasterName) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.Description = COALESCE(source.Description, target.Description),
                target.ParentId = COALESCE(source.ParentId, target.ParentId),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, MasterType, MasterName, MasterCode, Description, ParentId, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
            VALUES (source.TenantId, source.MasterType, source.MasterName, source.MasterCode, source.Description, source.ParentId, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0);

        UPDATE dbo.V_Stg_ItemType
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

-- SYNC 5: BRAND MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Brand
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        MERGE INTO dbo.V_Masters AS target
        USING (
            SELECT DISTINCT
                @TenantId AS TenantId,
                'BRAND' AS MasterType,
                RTRIM(LTRIM(s.Name)) AS MasterName,
                UPPER(REPLACE(RTRIM(LTRIM(s.Name)), ' ', '_')) AS MasterCode,
                s.Manufacturer AS Description
            FROM dbo.V_Stg_Brand s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = source.TenantId AND target.MasterType = source.MasterType AND LOWER(RTRIM(LTRIM(target.MasterName))) = LOWER(source.MasterName) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.Description = COALESCE(source.Description, target.Description),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, MasterType, MasterName, MasterCode, Description, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
            VALUES (source.TenantId, source.MasterType, source.MasterName, source.MasterCode, source.Description, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0);

        UPDATE dbo.V_Stg_Brand
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

-- SYNC 6: UNIT MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Unit
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        MERGE INTO dbo.V_Masters AS target
        USING (
            SELECT DISTINCT
                @TenantId AS TenantId,
                'UNIT' AS MasterType,
                RTRIM(LTRIM(s.Name)) AS MasterName,
                LOWER(RTRIM(LTRIM(s.Code))) AS MasterCode,
                s.Description
            FROM dbo.V_Stg_Unit s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = source.TenantId AND target.MasterType = source.MasterType AND (LOWER(target.MasterCode) = LOWER(source.MasterCode) OR LOWER(target.MasterName) = LOWER(source.MasterName)) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.Description = COALESCE(source.Description, target.Description),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, MasterType, MasterName, MasterCode, Description, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
            VALUES (source.TenantId, source.MasterType, source.MasterName, source.MasterCode, source.Description, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0);

        UPDATE dbo.V_Stg_Unit
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

-- SYNC 7: CUSTOMER MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Customer
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        MERGE INTO dbo.V_Customers AS target
        USING (
            SELECT
                s.RowNo,
                RTRIM(LTRIM(s.Name)) AS Name,
                RTRIM(LTRIM(s.Mobile)) AS Mobile,
                s.Village,
                s.Address,
                TRY_CAST(s.OpeningBalance AS DECIMAL(18,2)) AS OpeningBal,
                TRY_CAST(s.CreditLimit AS DECIMAL(18,2)) AS CreditLimit
            FROM dbo.V_Stg_Customer s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = @TenantId AND target.Mobile = source.Mobile AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.Name = source.Name,
                target.Village = COALESCE(source.Village, target.Village),
                target.Address = COALESCE(source.Address, target.Address),
                target.CreditLimit = COALESCE(source.CreditLimit, target.CreditLimit),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, Name, Mobile, Village, Address, OpeningBalance, CurrentBalance, CreditLimit, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
            VALUES (@TenantId, source.Name, source.Mobile, source.Village, source.Address, ISNULL(source.OpeningBal, 0), ISNULL(source.OpeningBal, 0), ISNULL(source.CreditLimit, 10000), 1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1');

        UPDATE dbo.V_Stg_Customer
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

-- SYNC 8: SUPPLIER MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Supplier
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        MERGE INTO dbo.V_Suppliers AS target
        USING (
            SELECT
                s.RowNo,
                RTRIM(LTRIM(s.Name)) AS Name,
                RTRIM(LTRIM(s.Mobile)) AS Mobile,
                s.Address,
                s.GSTNumber,
                TRY_CAST(s.OpeningBalance AS DECIMAL(18,2)) AS OpeningBal
            FROM dbo.V_Stg_Supplier s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = @TenantId AND LOWER(RTRIM(LTRIM(target.Name))) = LOWER(RTRIM(LTRIM(source.Name))) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.Mobile = COALESCE(source.Mobile, target.Mobile),
                target.Address = COALESCE(source.Address, target.Address),
                target.GSTNumber = COALESCE(source.GSTNumber, target.GSTNumber),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, Name, Mobile, Address, GSTNumber, OpeningBalance, CurrentBalance, IsActive, IsDeleted, Priority, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, EntrySource, IPAddress)
            VALUES (@TenantId, source.Name, source.Mobile, source.Address, source.GSTNumber, ISNULL(source.OpeningBal, 0), ISNULL(source.OpeningBal, 0), 1, 0, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 'Uploader', '127.0.0.1');

        UPDATE dbo.V_Stg_Supplier
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

-- SYNC 9: TAX / GST MASTER SYNC PROCEDURE
CREATE OR ALTER PROCEDURE dbo.usp_Sync_Tax
    @BatchId UNIQUEIDENTIFIER,
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        MERGE INTO dbo.V_Masters AS target
        USING (
            SELECT DISTINCT
                @TenantId AS TenantId,
                'Tax' AS MasterType,
                RTRIM(LTRIM(s.Name)) AS MasterName,
                UPPER(REPLACE(RTRIM(LTRIM(s.Name)), ' ', '')) AS MasterCode,
                RTRIM(LTRIM(s.RatePercent)) AS Description
            FROM dbo.V_Stg_Tax s
            WHERE s.BatchId = @BatchId AND s.RowStatus = 'Valid'
        ) AS source
        ON (target.TenantId = source.TenantId AND LOWER(target.MasterType) = 'tax' AND LOWER(RTRIM(LTRIM(target.MasterName))) = LOWER(source.MasterName) AND target.IsDeleted = 0)
        WHEN MATCHED THEN
            UPDATE SET
                target.Description = COALESCE(source.Description, target.Description),
                target.ModifiedDate = SYSUTCDATETIME(),
                target.ModifiedBy = @UserId
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (TenantId, MasterType, MasterName, MasterCode, Description, IsActive, IsDeleted, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate, SortOrder)
            VALUES (source.TenantId, source.MasterType, source.MasterName, source.MasterCode, source.Description, 1, 0, @UserId, SYSUTCDATETIME(), @UserId, SYSUTCDATETIME(), 0);

        UPDATE dbo.V_Stg_Tax
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

-- 7. BACKWARD COMPATIBILITY VIEWS (So old non-V_ references continue to work seamlessly)
IF OBJECT_ID('dbo.UploadType', 'V') IS NOT NULL DROP VIEW dbo.UploadType;
IF OBJECT_ID('dbo.UploadType', 'U') IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.UploadColumn', 'U') IS NOT NULL DROP TABLE dbo.UploadColumn;
    IF OBJECT_ID('dbo.UploadBatch', 'U') IS NOT NULL DROP TABLE dbo.UploadBatch;
    DROP TABLE dbo.UploadType;
END
GO
IF OBJECT_ID('dbo.Stg_Transaction', 'U') IS NOT NULL DROP TABLE dbo.Stg_Transaction;
IF OBJECT_ID('dbo.Stg_Item', 'U') IS NOT NULL DROP TABLE dbo.Stg_Item;
IF OBJECT_ID('dbo.Stg_ItemCategory', 'U') IS NOT NULL DROP TABLE dbo.Stg_ItemCategory;
IF OBJECT_ID('dbo.Stg_ItemType', 'U') IS NOT NULL DROP TABLE dbo.Stg_ItemType;
IF OBJECT_ID('dbo.Stg_Brand', 'U') IS NOT NULL DROP TABLE dbo.Stg_Brand;
IF OBJECT_ID('dbo.Stg_Unit', 'U') IS NOT NULL DROP TABLE dbo.Stg_Unit;
IF OBJECT_ID('dbo.Stg_Customer', 'U') IS NOT NULL DROP TABLE dbo.Stg_Customer;
IF OBJECT_ID('dbo.Stg_Supplier', 'U') IS NOT NULL DROP TABLE dbo.Stg_Supplier;
IF OBJECT_ID('dbo.Stg_Tax', 'U') IS NOT NULL DROP TABLE dbo.Stg_Tax;
IF OBJECT_ID('dbo.Stg_Stock', 'U') IS NOT NULL DROP TABLE dbo.Stg_Stock;
GO
CREATE OR ALTER VIEW dbo.UploadType AS SELECT * FROM dbo.V_UploadType;
GO
CREATE OR ALTER VIEW dbo.UploadColumn AS SELECT * FROM dbo.V_UploadColumn;
GO
CREATE OR ALTER VIEW dbo.UploadBatch AS SELECT * FROM dbo.V_UploadBatch;
GO
CREATE OR ALTER VIEW dbo.Stg_Transaction AS SELECT * FROM dbo.V_Stg_Transaction;
GO
CREATE OR ALTER VIEW dbo.Stg_Item AS SELECT * FROM dbo.V_Stg_Item;
GO
CREATE OR ALTER VIEW dbo.Stg_ItemCategory AS SELECT * FROM dbo.V_Stg_ItemCategory;
GO
CREATE OR ALTER VIEW dbo.Stg_ItemType AS SELECT * FROM dbo.V_Stg_ItemType;
GO
CREATE OR ALTER VIEW dbo.Stg_Brand AS SELECT * FROM dbo.V_Stg_Brand;
GO
CREATE OR ALTER VIEW dbo.Stg_Unit AS SELECT * FROM dbo.V_Stg_Unit;
GO
CREATE OR ALTER VIEW dbo.Stg_Customer AS SELECT * FROM dbo.V_Stg_Customer;
GO
CREATE OR ALTER VIEW dbo.Stg_Supplier AS SELECT * FROM dbo.V_Stg_Supplier;
GO
CREATE OR ALTER VIEW dbo.Stg_Tax AS SELECT * FROM dbo.V_Stg_Tax;
GO
CREATE OR ALTER VIEW dbo.Stg_Stock AS SELECT * FROM dbo.V_Stg_Stock;
GO
