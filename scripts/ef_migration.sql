IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [V_AuditLogs] (
    [ID] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [EntityName] nvarchar(max) NOT NULL,
    [EntityId] bigint NOT NULL,
    [OldValue] nvarchar(max) NULL,
    [NewValue] nvarchar(max) NULL,
    [DeviceId] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_AuditLogs] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_Customers] (
    [ID] bigint NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Mobile] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [Village] nvarchar(max) NULL,
    [CreditLimit] decimal(18,2) NOT NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_Customers] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_Expenses] (
    [ID] bigint NOT NULL IDENTITY,
    [ExpenseCategory] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ExpenseDate] datetime2 NOT NULL,
    [PaymentMode] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [AttachmentUrl] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_Expenses] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_Permissions] (
    [ID] bigint NOT NULL IDENTITY,
    [PermissionKey] nvarchar(max) NOT NULL,
    [Module] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_V_Permissions] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_Products] (
    [ID] bigint NOT NULL IDENTITY,
    [ProductCode] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NULL,
    [Brand] nvarchar(max) NULL,
    [Unit] nvarchar(max) NOT NULL,
    [Barcode] nvarchar(max) NULL,
    [PurchasePrice] decimal(18,2) NOT NULL,
    [SellingPrice] decimal(18,2) NOT NULL,
    [MRP] decimal(18,2) NOT NULL,
    [GSTPercent] decimal(5,2) NOT NULL,
    [OpeningStock] decimal(18,3) NOT NULL,
    [MinimumStock] decimal(18,3) NOT NULL,
    [CurrentStock] decimal(18,3) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_Products] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_Roles] (
    [ID] bigint NOT NULL IDENTITY,
    [RoleName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsSystemRole] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_Roles] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_Suppliers] (
    [ID] bigint NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Mobile] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [GSTNumber] nvarchar(max) NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_Suppliers] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_SyncQueues] (
    [ID] bigint NOT NULL IDENTITY,
    [ClientTransactionId] nvarchar(max) NOT NULL,
    [DeviceId] nvarchar(max) NULL,
    [UserId] bigint NOT NULL,
    [EntityName] nvarchar(max) NOT NULL,
    [Operation] nvarchar(max) NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    [SyncStatus] nvarchar(max) NOT NULL,
    [RetryCount] int NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_SyncQueues] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_TenantConfigurations] (
    [ID] bigint NOT NULL IDENTITY,
    [BrandingJson] nvarchar(max) NOT NULL,
    [FeatureJson] nvarchar(max) NOT NULL,
    [MenuJson] nvarchar(max) NOT NULL,
    [CustomFieldsJson] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_TenantConfigurations] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_Tenants] (
    [ID] bigint NOT NULL IDENTITY,
    [TenantCode] nvarchar(450) NOT NULL,
    [TenantName] nvarchar(max) NOT NULL,
    [OwnerName] nvarchar(max) NOT NULL,
    [Mobile] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [Village] nvarchar(max) NULL,
    [District] nvarchar(max) NULL,
    [State] nvarchar(max) NULL,
    [Pincode] nvarchar(max) NULL,
    [LogoUrl] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_V_Tenants] PRIMARY KEY ([ID])
);
GO

CREATE TABLE [V_Sales] (
    [ID] bigint NOT NULL IDENTITY,
    [InvoiceNumber] nvarchar(max) NOT NULL,
    [ClientTransactionId] nvarchar(450) NOT NULL,
    [CustomerId] bigint NULL,
    [SaleDate] datetime2 NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [UdhaarAmount] decimal(18,2) NOT NULL,
    [PaymentMode] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_Sales] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_V_Sales_V_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [V_Customers] ([ID])
);
GO

CREATE TABLE [V_UdhaarLedgers] (
    [ID] bigint NOT NULL IDENTITY,
    [CustomerId] bigint NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [TransactionType] nvarchar(max) NOT NULL,
    [SaleId] bigint NULL,
    [PaymentId] bigint NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [RunningBalance] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_UdhaarLedgers] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_V_UdhaarLedgers_V_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [V_Customers] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [V_RolePermissions] (
    [RoleId] bigint NOT NULL,
    [PermissionId] bigint NOT NULL,
    CONSTRAINT [PK_V_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_V_RolePermissions_V_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [V_Permissions] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_V_RolePermissions_V_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [V_Roles] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [V_Users] (
    [ID] bigint NOT NULL IDENTITY,
    [Username] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Mobile] nvarchar(max) NULL,
    [RoleId] bigint NOT NULL,
    [RefreshToken] nvarchar(max) NULL,
    [RefreshTokenExpiryTime] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_Users] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_V_Users_V_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [V_Roles] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [V_Payments] (
    [ID] bigint NOT NULL IDENTITY,
    [PaymentNumber] nvarchar(max) NOT NULL,
    [ClientTransactionId] nvarchar(max) NULL,
    [CustomerId] bigint NULL,
    [SupplierId] bigint NULL,
    [SaleId] bigint NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMode] nvarchar(max) NOT NULL,
    [TransactionReference] nvarchar(max) NULL,
    [PaymentDate] datetime2 NOT NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_Payments] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_V_Payments_V_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [V_Customers] ([ID]),
    CONSTRAINT [FK_V_Payments_V_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [V_Suppliers] ([ID])
);
GO

CREATE TABLE [V_SaleItems] (
    [ID] bigint NOT NULL IDENTITY,
    [SaleId] bigint NOT NULL,
    [ProductId] bigint NOT NULL,
    [ProductName] nvarchar(max) NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TaxPercent] decimal(5,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [CreatedBy] bigint NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedBy] bigint NOT NULL,
    [ModifiedDate] datetime2 NOT NULL,
    [DeletedBy] bigint NOT NULL,
    [DeletedDate] datetime2 NULL,
    [EntrySource] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [TenantId] bigint NOT NULL,
    CONSTRAINT [PK_V_SaleItems] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_V_SaleItems_V_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [V_Products] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_V_SaleItems_V_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [V_Sales] ([ID]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_V_Payments_CustomerId] ON [V_Payments] ([CustomerId]);
GO

CREATE INDEX [IX_V_Payments_SupplierId] ON [V_Payments] ([SupplierId]);
GO

CREATE INDEX [IX_V_RolePermissions_PermissionId] ON [V_RolePermissions] ([PermissionId]);
GO

CREATE INDEX [IX_V_SaleItems_ProductId] ON [V_SaleItems] ([ProductId]);
GO

CREATE INDEX [IX_V_SaleItems_SaleId] ON [V_SaleItems] ([SaleId]);
GO

CREATE UNIQUE INDEX [IX_V_Sales_ClientTransactionId] ON [V_Sales] ([ClientTransactionId]);
GO

CREATE INDEX [IX_V_Sales_CustomerId] ON [V_Sales] ([CustomerId]);
GO

CREATE UNIQUE INDEX [IX_V_Tenants_TenantCode] ON [V_Tenants] ([TenantCode]);
GO

CREATE INDEX [IX_V_UdhaarLedgers_CustomerId] ON [V_UdhaarLedgers] ([CustomerId]);
GO

CREATE INDEX [IX_V_Users_RoleId] ON [V_Users] ([RoleId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260924053856_InitialCreate', N'8.0.11');
GO

COMMIT;
GO

