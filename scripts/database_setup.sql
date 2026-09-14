-- =========================================================================================
-- VILLAGESHOP SAAS - FULL DATABASE CREATION, TABLES, CONSTRAINTS, PROCEDURES & SEED SCRIPT
-- SQL Server 2019 / 2022 / Azure SQL Compatible
-- =========================================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'VillageShopDb')
BEGIN
    CREATE DATABASE VillageShopDb;
END
GO

USE VillageShopDb;
GO

-- =========================================================================================
-- 1. DROP EXISTING TABLES IF NEEDED (In Reverse Dependency Order)
-- =========================================================================================
IF OBJECT_ID('dbo.SyncQueues', 'U') IS NOT NULL DROP TABLE dbo.SyncQueues;
IF OBJECT_ID('dbo.Expenses', 'U') IS NOT NULL DROP TABLE dbo.Expenses;
IF OBJECT_ID('dbo.UdhaarLedgers', 'U') IS NOT NULL DROP TABLE dbo.UdhaarLedgers;
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.SaleItems', 'U') IS NOT NULL DROP TABLE dbo.SaleItems;
IF OBJECT_ID('dbo.Sales', 'U') IS NOT NULL DROP TABLE dbo.Sales;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL DROP TABLE dbo.Suppliers;
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID('dbo.TenantConfigurations', 'U') IS NOT NULL DROP TABLE dbo.TenantConfigurations;
IF OBJECT_ID('dbo.RolePermissions', 'U') IS NOT NULL DROP TABLE dbo.RolePermissions;
IF OBJECT_ID('dbo.Permissions', 'U') IS NOT NULL DROP TABLE dbo.Permissions;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Tenants', 'U') IS NOT NULL DROP TABLE dbo.Tenants;
GO

-- =========================================================================================
-- 2. TENANTS TABLE
-- =========================================================================================
CREATE TABLE dbo.Tenants (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantCode NVARCHAR(50) NOT NULL,
    TenantName NVARCHAR(150) NOT NULL,
    OwnerName NVARCHAR(100) NOT NULL,
    Mobile NVARCHAR(20) NOT NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(250) NULL,
    Village NVARCHAR(100) NULL,
    District NVARCHAR(100) NULL,
    State NVARCHAR(100) NULL,
    Pincode NVARCHAR(20) NULL,
    LogoUrl NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Tenants_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Tenants_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Tenants_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Tenants_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Tenants_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Tenants_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Tenants_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Tenants_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Tenants_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Tenants_IPAddress DEFAULT ''
);
CREATE UNIQUE INDEX UX_Tenants_TenantCode ON dbo.Tenants(TenantCode);
GO

-- =========================================================================================
-- 3. ROLES TABLE
-- =========================================================================================
CREATE TABLE dbo.Roles (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    RoleName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(250) NULL,
    IsSystemRole BIT NOT NULL CONSTRAINT DF_Roles_IsSystemRole DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Roles_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Roles_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Roles_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Roles_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Roles_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Roles_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Roles_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Roles_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Roles_IPAddress DEFAULT ''
);
CREATE INDEX IX_Roles_TenantId ON dbo.Roles(TenantId);
GO

-- =========================================================================================
-- 4. USERS TABLE
-- =========================================================================================
CREATE TABLE dbo.Users (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NULL,
    PasswordHash NVARCHAR(250) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Mobile NVARCHAR(20) NULL,
    RoleId BIGINT NOT NULL,
    RefreshToken NVARCHAR(250) NULL,
    RefreshTokenExpiryTime DATETIME2 NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Users_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Users_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Users_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Users_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Users_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Users_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Users_IPAddress DEFAULT '',
    CONSTRAINT FK_Users_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(ID),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(ID)
);
CREATE INDEX IX_Users_TenantId_Username ON dbo.Users(TenantId, Username);
GO

-- =========================================================================================
-- 5. PERMISSIONS & ROLEPERMISSIONS TABLES
-- =========================================================================================
CREATE TABLE dbo.Permissions (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PermissionKey NVARCHAR(100) NOT NULL,
    Module NVARCHAR(50) NOT NULL,
    Description NVARCHAR(250) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Permissions_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Permissions_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Permissions_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Permissions_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Permissions_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Permissions_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Permissions_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Permissions_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Permissions_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Permissions_IPAddress DEFAULT ''
);
CREATE UNIQUE INDEX UX_Permissions_Key ON dbo.Permissions(PermissionKey);

CREATE TABLE dbo.RolePermissions (
    RoleId BIGINT NOT NULL,
    PermissionId BIGINT NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(ID) ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(ID) ON DELETE CASCADE
);
GO

-- =========================================================================================
-- 6. TENANT CONFIGURATIONS TABLE (Branding, Features, Menus, Dynamic Custom Fields)
-- =========================================================================================
CREATE TABLE dbo.TenantConfigurations (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    BrandingJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TenantConfig_Branding DEFAULT '{}',
    FeatureJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TenantConfig_Feature DEFAULT '{}',
    MenuJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TenantConfig_Menu DEFAULT '[]',
    CustomFieldsJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TenantConfig_CustomFields DEFAULT '[]',
    IsActive BIT NOT NULL CONSTRAINT DF_TenantConfigurations_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_TenantConfigurations_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_TenantConfigurations_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_TenantConfigurations_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_TenantConfigurations_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_TenantConfigurations_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_TenantConfigurations_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_TenantConfigurations_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_TenantConfigurations_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_TenantConfigurations_IPAddress DEFAULT '',
    CONSTRAINT FK_TenantConfigurations_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(ID)
);
CREATE UNIQUE INDEX UX_TenantConfigurations_TenantId ON dbo.TenantConfigurations(TenantId);
GO

-- =========================================================================================
-- 7. AUDIT LOGS TABLE
-- =========================================================================================
CREATE TABLE dbo.AuditLogs (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityId BIGINT NOT NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    DeviceId NVARCHAR(100) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_AuditLogs_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_AuditLogs_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_AuditLogs_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_AuditLogs_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_AuditLogs_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_AuditLogs_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_AuditLogs_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_AuditLogs_IPAddress DEFAULT ''
);
CREATE INDEX IX_AuditLogs_TenantId_CreatedDate ON dbo.AuditLogs(TenantId, CreatedDate DESC);
GO

-- =========================================================================================
-- 8. PRODUCTS TABLE
-- =========================================================================================
CREATE TABLE dbo.Products (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    ProductCode NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Category NVARCHAR(100) NULL,
    Brand NVARCHAR(100) NULL,
    Unit NVARCHAR(20) NOT NULL CONSTRAINT DF_Products_Unit DEFAULT 'pcs',
    Barcode NVARCHAR(100) NULL,
    PurchasePrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_PurchasePrice DEFAULT 0,
    SellingPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_SellingPrice DEFAULT 0,
    MRP DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_MRP DEFAULT 0,
    GSTPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_Products_GSTPercent DEFAULT 0,
    OpeningStock DECIMAL(18,3) NOT NULL CONSTRAINT DF_Products_OpeningStock DEFAULT 0,
    MinimumStock DECIMAL(18,3) NOT NULL CONSTRAINT DF_Products_MinimumStock DEFAULT 0,
    CurrentStock DECIMAL(18,3) NOT NULL CONSTRAINT DF_Products_CurrentStock DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Products_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Products_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Products_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Products_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Products_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Products_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Products_IPAddress DEFAULT '',
    CONSTRAINT FK_Products_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(ID)
);
CREATE INDEX IX_Products_TenantId_Name ON dbo.Products(TenantId, Name);
CREATE INDEX IX_Products_TenantId_Barcode ON dbo.Products(TenantId, Barcode);
GO

-- =========================================================================================
-- 9. CUSTOMERS TABLE
-- =========================================================================================
CREATE TABLE dbo.Customers (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Mobile NVARCHAR(20) NULL,
    Address NVARCHAR(250) NULL,
    Village NVARCHAR(100) NULL,
    CreditLimit DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customers_CreditLimit DEFAULT 0,
    OpeningBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customers_OpeningBalance DEFAULT 0,
    CurrentBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customers_CurrentBalance DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Customers_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Customers_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Customers_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Customers_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Customers_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Customers_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Customers_IPAddress DEFAULT '',
    CONSTRAINT FK_Customers_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(ID)
);
CREATE INDEX IX_Customers_TenantId_Mobile ON dbo.Customers(TenantId, Mobile);
GO

-- =========================================================================================
-- 10. SUPPLIERS TABLE
-- =========================================================================================
CREATE TABLE dbo.Suppliers (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Mobile NVARCHAR(20) NULL,
    Address NVARCHAR(250) NULL,
    GSTNumber NVARCHAR(50) NULL,
    OpeningBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Suppliers_OpeningBalance DEFAULT 0,
    CurrentBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Suppliers_CurrentBalance DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Suppliers_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Suppliers_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Suppliers_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Suppliers_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Suppliers_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Suppliers_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Suppliers_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Suppliers_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Suppliers_IPAddress DEFAULT '',
    CONSTRAINT FK_Suppliers_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(ID)
);
CREATE INDEX IX_Suppliers_TenantId_Name ON dbo.Suppliers(TenantId, Name);
GO

-- =========================================================================================
-- 11. SALES & SALEITEMS TABLES (WITH IDEMPOTENCY CLIENT TRANSACTION ID UNIQUE INDEX)
-- =========================================================================================
CREATE TABLE dbo.Sales (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    ClientTransactionId NVARCHAR(100) NOT NULL, -- Unique Client GUID for offline sync idempotency
    CustomerId BIGINT NULL,
    SaleDate DATETIME2 NOT NULL CONSTRAINT DF_Sales_SaleDate DEFAULT GETUTCDATE(),
    SubTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_SubTotal DEFAULT 0,
    TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_TaxAmount DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_DiscountAmount DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_TotalAmount DEFAULT 0,
    PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_PaidAmount DEFAULT 0,
    UdhaarAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_UdhaarAmount DEFAULT 0,
    PaymentMode NVARCHAR(50) NOT NULL CONSTRAINT DF_Sales_PaymentMode DEFAULT 'Cash',
    Notes NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Sales_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Sales_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Sales_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Sales_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Sales_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Sales_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Sales_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Sales_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Sales_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Sales_IPAddress DEFAULT '',
    CONSTRAINT FK_Sales_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(ID),
    CONSTRAINT FK_Sales_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(ID)
);
CREATE UNIQUE INDEX UX_Sales_ClientTransactionId ON dbo.Sales(ClientTransactionId);
CREATE INDEX IX_Sales_TenantId_SaleDate ON dbo.Sales(TenantId, SaleDate DESC);

CREATE TABLE dbo.SaleItems (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    SaleId BIGINT NOT NULL,
    ProductId BIGINT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL CONSTRAINT DF_SaleItems_Quantity DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_SaleItems_UnitPrice DEFAULT 0,
    TaxPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_SaleItems_TaxPercent DEFAULT 0,
    TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SaleItems_TaxAmount DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SaleItems_TotalAmount DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_SaleItems_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_SaleItems_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_SaleItems_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_SaleItems_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_SaleItems_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_SaleItems_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_SaleItems_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_SaleItems_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_SaleItems_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_SaleItems_IPAddress DEFAULT '',
    CONSTRAINT FK_SaleItems_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales(ID) ON DELETE CASCADE,
    CONSTRAINT FK_SaleItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ID)
);
CREATE INDEX IX_SaleItems_SaleId ON dbo.SaleItems(SaleId);
GO

-- =========================================================================================
-- 12. PAYMENTS & UDHAAR LEDGERS TABLES
-- =========================================================================================
CREATE TABLE dbo.Payments (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    PaymentNumber NVARCHAR(50) NOT NULL,
    ClientTransactionId NVARCHAR(100) NULL,
    CustomerId BIGINT NULL,
    SupplierId BIGINT NULL,
    SaleId BIGINT NULL,
    Amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payments_Amount DEFAULT 0,
    PaymentMode NVARCHAR(50) NOT NULL CONSTRAINT DF_Payments_PaymentMode DEFAULT 'Cash',
    TransactionReference NVARCHAR(100) NULL,
    PaymentDate DATETIME2 NOT NULL CONSTRAINT DF_Payments_PaymentDate DEFAULT GETUTCDATE(),
    Notes NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Payments_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Payments_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Payments_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Payments_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Payments_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Payments_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Payments_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Payments_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Payments_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Payments_IPAddress DEFAULT '',
    CONSTRAINT FK_Payments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(ID)
);

CREATE TABLE dbo.UdhaarLedgers (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    CustomerId BIGINT NOT NULL,
    TransactionDate DATETIME2 NOT NULL CONSTRAINT DF_UdhaarLedgers_TransactionDate DEFAULT GETUTCDATE(),
    TransactionType NVARCHAR(50) NOT NULL, -- CREDIT_SALE or PAYMENT_RECEIVED
    SaleId BIGINT NULL,
    PaymentId BIGINT NULL,
    DebitAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_UdhaarLedgers_DebitAmount DEFAULT 0,
    CreditAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_UdhaarLedgers_CreditAmount DEFAULT 0,
    RunningBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_UdhaarLedgers_RunningBalance DEFAULT 0,
    Description NVARCHAR(250) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_UdhaarLedgers_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_UdhaarLedgers_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_UdhaarLedgers_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_UdhaarLedgers_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_UdhaarLedgers_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_UdhaarLedgers_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_UdhaarLedgers_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_UdhaarLedgers_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_UdhaarLedgers_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_UdhaarLedgers_IPAddress DEFAULT '',
    CONSTRAINT FK_UdhaarLedgers_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(ID)
);
CREATE INDEX IX_UdhaarLedgers_CustomerId_Date ON dbo.UdhaarLedgers(CustomerId, TransactionDate DESC);
GO

-- =========================================================================================
-- 13. EXPENSES & SYNCQUEUES TABLES
-- =========================================================================================
CREATE TABLE dbo.Expenses (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    ExpenseCategory NVARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Expenses_Amount DEFAULT 0,
    ExpenseDate DATETIME2 NOT NULL CONSTRAINT DF_Expenses_ExpenseDate DEFAULT GETUTCDATE(),
    PaymentMode NVARCHAR(50) NOT NULL CONSTRAINT DF_Expenses_PaymentMode DEFAULT 'Cash',
    Description NVARCHAR(500) NULL,
    AttachmentUrl NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Expenses_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Expenses_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_Expenses_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_Expenses_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Expenses_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_Expenses_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Expenses_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_Expenses_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_Expenses_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_Expenses_IPAddress DEFAULT '',
    CONSTRAINT FK_Expenses_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(ID)
);

CREATE TABLE dbo.SyncQueues (
    ID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    ClientTransactionId NVARCHAR(100) NOT NULL,
    DeviceId NVARCHAR(100) NULL,
    UserId BIGINT NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    Operation NVARCHAR(20) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    SyncStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_SyncQueues_SyncStatus DEFAULT 'PENDING',
    RetryCount INT NOT NULL CONSTRAINT DF_SyncQueues_RetryCount DEFAULT 0,
    ErrorMessage NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_SyncQueues_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_SyncQueues_IsDeleted DEFAULT 0,
    Priority INT NOT NULL CONSTRAINT DF_SyncQueues_Priority DEFAULT 0,
    CreatedBy BIGINT NOT NULL CONSTRAINT DF_SyncQueues_CreatedBy DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_SyncQueues_CreatedDate DEFAULT GETUTCDATE(),
    ModifiedBy BIGINT NOT NULL CONSTRAINT DF_SyncQueues_ModifiedBy DEFAULT 0,
    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_SyncQueues_ModifiedDate DEFAULT GETUTCDATE(),
    DeletedBy BIGINT NOT NULL CONSTRAINT DF_SyncQueues_DeletedBy DEFAULT 0,
    DeletedDate DATETIME2 NULL,
    EntrySource NVARCHAR(50) NOT NULL CONSTRAINT DF_SyncQueues_EntrySource DEFAULT 'SYSTEM',
    IPAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_SyncQueues_IPAddress DEFAULT ''
);
CREATE UNIQUE INDEX UX_SyncQueues_ClientTx ON dbo.SyncQueues(ClientTransactionId);
GO

-- =========================================================================================
-- 14. STORED PROCEDURES FOR DAPPER FAST REPORTING
-- =========================================================================================

-- Procedure 1: Daily Sales & Financial Report
IF OBJECT_ID('dbo.sp_GetDailySalesReport', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetDailySalesReport;
GO
CREATE PROCEDURE dbo.sp_GetDailySalesReport
    @TenantId BIGINT,
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        CAST(SaleDate AS DATE) AS SaleDateOnly,
        COUNT(ID) AS TotalInvoices,
        SUM(SubTotal) AS GrossSales,
        SUM(DiscountAmount) AS TotalDiscounts,
        SUM(TaxAmount) AS TotalTax,
        SUM(TotalAmount) AS NetSales,
        SUM(PaidAmount) AS CashAndOnlineReceived,
        SUM(UdhaarAmount) AS NewUdhaarGiven,
        SUM(CASE WHEN PaymentMode = 'Cash' THEN PaidAmount ELSE 0 END) AS CashAmount,
        SUM(CASE WHEN PaymentMode = 'UPI' THEN PaidAmount ELSE 0 END) AS UpiAmount
    FROM dbo.Sales WITH (NOLOCK)
    WHERE TenantId = @TenantId
      AND IsDeleted = 0
      AND SaleDate >= @StartDate AND SaleDate <= @EndDate
    GROUP BY CAST(SaleDate AS DATE)
    ORDER BY SaleDateOnly DESC;
END;
GO

-- Procedure 2: Customer Udhaar Ledger Summary
IF OBJECT_ID('dbo.sp_GetCustomerUdhaarLedger', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetCustomerUdhaarLedger;
GO
CREATE PROCEDURE dbo.sp_GetCustomerUdhaarLedger
    @TenantId BIGINT,
    @CustomerId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        c.ID AS CustomerId,
        c.Name AS CustomerName,
        c.Mobile,
        c.Village,
        c.CreditLimit,
        c.CurrentBalance AS OutstandingUdhaar,
        u.TransactionDate,
        u.TransactionType,
        u.DebitAmount,
        u.CreditAmount,
        u.RunningBalance,
        u.Description
    FROM dbo.Customers c WITH (NOLOCK)
    LEFT JOIN dbo.UdhaarLedgers u WITH (NOLOCK) ON c.ID = u.CustomerId AND u.IsDeleted = 0
    WHERE c.TenantId = @TenantId 
      AND c.ID = @CustomerId 
      AND c.IsDeleted = 0
    ORDER BY u.TransactionDate DESC;
END;
GO

-- Procedure 3: Low Stock Alerts Report
IF OBJECT_ID('dbo.sp_GetLowStockProducts', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetLowStockProducts;
GO
CREATE PROCEDURE dbo.sp_GetLowStockProducts
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ID AS ProductId,
        ProductCode,
        Name AS ProductName,
        Category,
        Unit,
        CurrentStock,
        MinimumStock,
        (MinimumStock - CurrentStock) AS ShortageQuantity,
        PurchasePrice,
        SellingPrice
    FROM dbo.Products WITH (NOLOCK)
    WHERE TenantId = @TenantId
      AND IsDeleted = 0
      AND IsActive = 1
      AND CurrentStock <= MinimumStock
    ORDER BY (MinimumStock - CurrentStock) DESC;
END;
GO

-- Procedure 4: Tenant Dashboard KPI Aggregations
IF OBJECT_ID('dbo.sp_GetTenantDashboardSummary', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetTenantDashboardSummary;
GO
CREATE PROCEDURE dbo.sp_GetTenantDashboardSummary
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TodayStart DATETIME2 = CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2);

    SELECT 
        ISNULL((SELECT SUM(TotalAmount) FROM dbo.Sales WHERE TenantId = @TenantId AND IsDeleted = 0 AND SaleDate >= @TodayStart), 0) AS TodaysSales,
        ISNULL((SELECT SUM(CurrentBalance) FROM dbo.Customers WHERE TenantId = @TenantId AND IsDeleted = 0), 0) AS TotalUdhaarOutstanding,
        ISNULL((SELECT COUNT(ID) FROM dbo.Products WHERE TenantId = @TenantId AND IsDeleted = 0 AND CurrentStock <= MinimumStock), 0) AS LowStockCount,
        ISNULL((SELECT COUNT(ID) FROM dbo.Customers WHERE TenantId = @TenantId AND IsDeleted = 0), 0) AS TotalCustomers,
        ISNULL((SELECT COUNT(ID) FROM dbo.Products WHERE TenantId = @TenantId AND IsDeleted = 0), 0) AS TotalProducts;
END;
GO

-- =========================================================================================
-- 15. SEED DATA SCRIPT (Default System Roles, Tenant, Admin User & Configuration)
-- =========================================================================================
SET IDENTITY_INSERT dbo.Tenants ON;
IF NOT EXISTS (SELECT * FROM dbo.Tenants WHERE ID = 1)
BEGIN
    INSERT INTO dbo.Tenants (ID, TenantCode, TenantName, OwnerName, Mobile, Email, Village, District, State, Pincode)
    VALUES (1, 'DEMO_SHOP', 'Sharma General Store', 'Ramesh Sharma', '9876543210', 'ramesh@villageshop.in', 'Rampur', 'Meerut', 'Uttar Pradesh', '250001');
END
SET IDENTITY_INSERT dbo.Tenants OFF;

SET IDENTITY_INSERT dbo.Roles ON;
IF NOT EXISTS (SELECT * FROM dbo.Roles WHERE ID = 1)
BEGIN
    INSERT INTO dbo.Roles (ID, TenantId, RoleName, Description, IsSystemRole)
    VALUES (1, 1, 'Admin', 'Tenant Admin Role', 1);
END
SET IDENTITY_INSERT dbo.Roles OFF;

SET IDENTITY_INSERT dbo.Users ON;
IF NOT EXISTS (SELECT * FROM dbo.Users WHERE Username = 'admin' AND TenantId = 1)
BEGIN
    -- Default Admin Password: admin123 (Valid BCrypt hash)
    INSERT INTO dbo.Users (ID, TenantId, Username, Email, PasswordHash, FullName, Mobile, RoleId)
    VALUES (1, 1, 'admin', 'ramesh@villageshop.in', '$2a$11$ahtmNRK.5eUxLK4OEFlG6eAArhJynM0cEpxPnuucuhr3ERLh2nGf6', 'Ramesh Sharma', '9876543210', 1);
END
SET IDENTITY_INSERT dbo.Users OFF;

IF NOT EXISTS (SELECT * FROM dbo.TenantConfigurations WHERE TenantId = 1)
BEGIN
    INSERT INTO dbo.TenantConfigurations (TenantId, BrandingJson, FeatureJson, MenuJson)
    VALUES (
        1,
        '{"appName":"Sharma General Store","primaryColor":"#2563EB","secondaryColor":"#16A34A"}',
        '{"Sales":true,"Purchase":true,"Stock":true,"Udhaar":true,"Payments":true,"Expenses":true,"GST":true,"Barcode":true}',
        '[{"id":1,"title":"Dashboard","icon":"dashboard","path":"/dashboard"},{"id":2,"title":"Sales","icon":"shopping_cart","path":"/sales"},{"id":3,"title":"Products","icon":"inventory","path":"/products"},{"id":4,"title":"Customers","icon":"people","path":"/customers"}]'
    );
END
GO

PRINT '=========================================================================================';
PRINT 'VILLAGESHOP DATABASE SETUP COMPLETED SUCCESSFULLY!';
PRINT '=========================================================================================';
