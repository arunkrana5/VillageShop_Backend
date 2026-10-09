using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using VillageShop.Api.Middleware;
using VillageShop.Application.Auth.Services;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.Items.Services;
using VillageShop.Application.StockIn.Services;
using VillageShop.Application.Sales.Services;
using VillageShop.Domain.Entities;
using VillageShop.Infrastructure.Dapper;
using VillageShop.Infrastructure.EFCore;
using VillageShop.Infrastructure.Security;
using VillageShop.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000", "http://0.0.0.0:8080");

// Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQL Server DB Context (Pinned to 185.100.212.57,1433 dev.inhouse)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Server=185.100.212.57,1433;Database=dev.inhouse;User Id=sa;Password=Thrivera@1701;TrustServerCertificate=True;Encrypt=False;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOpts =>
    {
        sqlOpts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
    });
});

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IDapperContext, DapperContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Application Services DI
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ISaleService, SaleService>();

// JWT Authentication Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "VillageShopSecretSuperSecureKey_2026_MustBeLongEnough!";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Ensure Database & Tables are created on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try { db.Database.EnsureCreated(); } catch (Exception) {}

        // Ensure V_Items table exists in SQL Server / database
        try
        {
            var createVItemsSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Items')
BEGIN
    CREATE TABLE [V_Items] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [TenantId] bigint NOT NULL,
        [ItemCode] nvarchar(100) NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Category] nvarchar(150) NULL,
        [Unit] nvarchar(50) NOT NULL DEFAULT 'pcs',
        [Format] nvarchar(50) NOT NULL DEFAULT 'Packed',
        [Description] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [Priority] int NOT NULL DEFAULT 0,
        [CreatedBy] bigint NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedBy] bigint NOT NULL DEFAULT 0,
        [ModifiedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [DeletedBy] bigint NOT NULL DEFAULT 0,
        [DeletedDate] datetime2 NULL,
        [EntrySource] nvarchar(100) NOT NULL DEFAULT '',
        [IPAddress] nvarchar(100) NOT NULL DEFAULT '',
        CONSTRAINT [PK_V_Items] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END";
            db.Database.ExecuteSqlRaw(createVItemsSql);

            var createVStockSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Stock')
BEGIN
    CREATE TABLE [V_Stock] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [TenantId] bigint NOT NULL,
        [ItemId] bigint NULL,
        [ProductCode] nvarchar(100) NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Category] nvarchar(150) NULL,
        [Brand] nvarchar(150) NULL,
        [Unit] nvarchar(50) NOT NULL DEFAULT 'pcs',
        [Barcode] nvarchar(100) NULL,
        [PurchasePrice] decimal(18,2) NOT NULL DEFAULT 0,
        [SellingPrice] decimal(18,2) NOT NULL DEFAULT 0,
        [MRP] decimal(18,2) NOT NULL DEFAULT 0,
        [GSTPercent] decimal(5,2) NOT NULL DEFAULT 0,
        [OpeningStock] decimal(18,3) NOT NULL DEFAULT 0,
        [MinimumStock] decimal(18,3) NOT NULL DEFAULT 0,
        [CurrentStock] decimal(18,3) NOT NULL DEFAULT 0,
        [BatchNumber] nvarchar(100) NULL,
        [RackNumber] nvarchar(100) NULL,
        [ExpiryDate] datetime2 NULL,
        [HSNCode] nvarchar(100) NULL,
        [ImageUrl] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [Priority] int NOT NULL DEFAULT 0,
        [CreatedBy] bigint NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedBy] bigint NOT NULL DEFAULT 0,
        [ModifiedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [DeletedBy] bigint NOT NULL DEFAULT 0,
        [DeletedDate] datetime2 NULL,
        [EntrySource] nvarchar(100) NOT NULL DEFAULT '',
        [IPAddress] nvarchar(100) NOT NULL DEFAULT '',
        CONSTRAINT [PK_V_Stock] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Stock') AND name = 'ItemId')
    BEGIN
        ALTER TABLE [V_Stock] ADD [ItemId] bigint NULL;
    END
END";
            db.Database.ExecuteSqlRaw(createVStockSql);

            var alterVCustomersSql = @"
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Customers')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Customers') AND name = 'Email')
    BEGIN
        ALTER TABLE [V_Customers] ADD [Email] nvarchar(max) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Customers') AND name = 'WhatsApp')
    BEGIN
        ALTER TABLE [V_Customers] ADD [WhatsApp] nvarchar(max) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Customers') AND name = 'FatherName')
    BEGIN
        ALTER TABLE [V_Customers] ADD [FatherName] nvarchar(max) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Customers') AND name = 'Address')
    BEGIN
        ALTER TABLE [V_Customers] ADD [Address] nvarchar(max) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Customers') AND name = 'PO')
    BEGIN
        ALTER TABLE [V_Customers] ADD [PO] nvarchar(max) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Customers') AND name = 'PS')
    BEGIN
        ALTER TABLE [V_Customers] ADD [PS] nvarchar(max) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Customers') AND name = 'Dist')
    BEGIN
        ALTER TABLE [V_Customers] ADD [Dist] nvarchar(max) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Customers') AND name = 'Pincode')
    BEGIN
        ALTER TABLE [V_Customers] ADD [Pincode] nvarchar(max) NULL;
    END
END";
            db.Database.ExecuteSqlRaw(alterVCustomersSql);

            var createVSalesSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Sales')
BEGIN
    CREATE TABLE [V_Sales] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [TenantId] bigint NOT NULL,
        [InvoiceNumber] nvarchar(100) NULL,
        [ClientTransactionId] nvarchar(100) NULL,
        [CustomerId] bigint NULL,
        [SaleDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [SubTotal] decimal(18,2) NOT NULL DEFAULT 0,
        [TaxAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [PaidAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [UdhaarAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [PaymentMode] nvarchar(100) NOT NULL DEFAULT 'Cash',
        [Notes] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [Priority] int NOT NULL DEFAULT 0,
        [CreatedBy] bigint NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedBy] bigint NOT NULL DEFAULT 0,
        [ModifiedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [DeletedBy] bigint NOT NULL DEFAULT 0,
        [DeletedDate] datetime2 NULL,
        [EntrySource] nvarchar(100) NOT NULL DEFAULT '',
        [IPAddress] nvarchar(100) NOT NULL DEFAULT '',
        CONSTRAINT [PK_V_Sales] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END";
            db.Database.ExecuteSqlRaw(createVSalesSql);

            var createVSaleItemsSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_SaleItems')
BEGIN
    CREATE TABLE [V_SaleItems] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [TenantId] bigint NOT NULL,
        [SaleId] bigint NOT NULL,
        [ProductId] bigint NOT NULL DEFAULT 0,
        [ProductName] nvarchar(250) NOT NULL DEFAULT '',
        [Quantity] decimal(18,3) NOT NULL DEFAULT 1,
        [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0,
        [TaxPercent] decimal(5,2) NOT NULL DEFAULT 0,
        [TaxAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [Priority] int NOT NULL DEFAULT 0,
        [CreatedBy] bigint NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedBy] bigint NOT NULL DEFAULT 0,
        [ModifiedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [DeletedBy] bigint NOT NULL DEFAULT 0,
        [DeletedDate] datetime2 NULL,
        [EntrySource] nvarchar(100) NOT NULL DEFAULT '',
        [IPAddress] nvarchar(100) NOT NULL DEFAULT '',
        CONSTRAINT [PK_V_SaleItems] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END";
            db.Database.ExecuteSqlRaw(createVSaleItemsSql);

            var createVSalePaymentsSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_SalePayments')
BEGIN
    CREATE TABLE [V_SalePayments] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [TenantId] bigint NOT NULL,
        [SaleId] bigint NOT NULL,
        [PaymentMode] nvarchar(100) NOT NULL DEFAULT 'Cash',
        [Amount] decimal(18,2) NOT NULL DEFAULT 0,
        [UpiIdUsed] nvarchar(250) NULL,
        [AccountName] nvarchar(250) NULL,
        [BankName] nvarchar(250) NULL,
        [TransactionRef] nvarchar(250) NULL,
        [IsReceived] bit NOT NULL DEFAULT 1,
        [Notes] nvarchar(max) NULL,
        [PaymentDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [IsActive] bit NOT NULL DEFAULT 1,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [Priority] int NOT NULL DEFAULT 0,
        [CreatedBy] bigint NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedBy] bigint NOT NULL DEFAULT 0,
        [ModifiedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [DeletedBy] bigint NOT NULL DEFAULT 0,
        [DeletedDate] datetime2 NULL,
        [EntrySource] nvarchar(100) NOT NULL DEFAULT '',
        [IPAddress] nvarchar(100) NOT NULL DEFAULT '',
        CONSTRAINT [PK_V_SalePayments] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_SalePayments') AND name = 'UpiIdUsed')
    BEGIN
        ALTER TABLE [V_SalePayments] ADD [UpiIdUsed] nvarchar(250) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_SalePayments') AND name = 'AccountName')
    BEGIN
        ALTER TABLE [V_SalePayments] ADD [AccountName] nvarchar(250) NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_SalePayments') AND name = 'BankName')
    BEGIN
        ALTER TABLE [V_SalePayments] ADD [BankName] nvarchar(250) NULL;
    END
END";
            db.Database.ExecuteSqlRaw(createVSalePaymentsSql);

            var reconcileUdhaarLedgersSql = @"
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'V_UdhaarLedgers')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_UdhaarLedgers') AND name = 'SaleId')
    BEGIN
        ALTER TABLE [V_UdhaarLedgers] ADD [SaleId] bigint NULL;
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_UdhaarLedgers') AND name = 'PaymentId')
    BEGIN
        ALTER TABLE [V_UdhaarLedgers] ADD [PaymentId] bigint NULL;
    END
END";
            db.Database.ExecuteSqlRaw(reconcileUdhaarLedgersSql);

            var createMastersSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Masters')
BEGIN
    CREATE TABLE [V_Masters] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [TenantId] bigint NOT NULL DEFAULT 1,
        [MasterType] nvarchar(100) NOT NULL,
        [MasterName] nvarchar(250) NOT NULL,
        [MasterCode] nvarchar(100) NOT NULL DEFAULT '',
        [Description] nvarchar(max) NULL,
        [ParentId] bigint NULL,
        [SortOrder] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [Priority] int NOT NULL DEFAULT 0,
        [CreatedBy] bigint NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedBy] bigint NOT NULL DEFAULT 0,
        [ModifiedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [DeletedBy] bigint NOT NULL DEFAULT 0,
        [DeletedDate] datetime2 NULL,
        [EntrySource] nvarchar(100) NOT NULL DEFAULT '',
        [IPAddress] nvarchar(100) NOT NULL DEFAULT '',
        CONSTRAINT [PK_V_Masters] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'V_ItemCategories')
BEGIN
    INSERT INTO [V_Masters] ([TenantId], [MasterType], [MasterName], [MasterCode], [Description], [Priority], [IsActive], [IsDeleted], [CreatedDate])
    SELECT [TenantId], 'ItemCategory', [CategoryName], ISNULL([CategoryCode], ''), [Description], [Priority], [IsActive], [IsDeleted], ISNULL([CreatedDate], GETUTCDATE())
    FROM [V_ItemCategories]
    WHERE NOT EXISTS (SELECT 1 FROM [V_Masters] WHERE [MasterType] = 'ItemCategory' AND [MasterName] = [V_ItemCategories].[CategoryName]);
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'V_UnitOfMeasurements')
BEGIN
    INSERT INTO [V_Masters] ([TenantId], [MasterType], [MasterName], [MasterCode], [Description], [Priority], [IsActive], [IsDeleted], [CreatedDate])
    SELECT [TenantId], 'UnitOfMeasurement', [UOMName], ISNULL([UOMCode], ''), [Description], [Priority], [IsActive], [IsDeleted], ISNULL([CreatedDate], GETUTCDATE())
    FROM [V_UnitOfMeasurements]
    WHERE NOT EXISTS (SELECT 1 FROM [V_Masters] WHERE [MasterType] = 'UnitOfMeasurement' AND [MasterName] = [V_UnitOfMeasurements].[UOMName]);
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'V_ItemTypes')
BEGIN
    INSERT INTO [V_Masters] ([TenantId], [MasterType], [MasterName], [MasterCode], [Description], [Priority], [IsActive], [IsDeleted], [CreatedDate])
    SELECT [TenantId], 'ItemType', [ItemTypeName], ISNULL([ItemTypeCode], ''), [Description], [Priority], [IsActive], [IsDeleted], ISNULL([CreatedDate], GETUTCDATE())
    FROM [V_ItemTypes]
    WHERE NOT EXISTS (SELECT 1 FROM [V_Masters] WHERE [MasterType] = 'ItemType' AND [MasterName] = [V_ItemTypes].[ItemTypeName]);
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Brands')
BEGIN
    INSERT INTO [V_Masters] ([TenantId], [MasterType], [MasterName], [MasterCode], [Description], [Priority], [IsActive], [IsDeleted], [CreatedDate])
    SELECT [TenantId], 'Brand', [BrandName], ISNULL([BrandCode], ''), [Description], [Priority], [IsActive], [IsDeleted], ISNULL([CreatedDate], GETUTCDATE())
    FROM [V_Brands]
    WHERE NOT EXISTS (SELECT 1 FROM [V_Masters] WHERE [MasterType] = 'Brand' AND [MasterName] = [V_Brands].[BrandName]);
END

-- Seed Default Masters if V_Masters is empty
IF NOT EXISTS (SELECT 1 FROM [V_Masters] WHERE [MasterType] = 'ItemCategory')
BEGIN
    INSERT INTO [V_Masters] ([TenantId], [MasterType], [MasterName], [MasterCode], [Description], [Priority])
    VALUES 
    (1, 'ItemCategory', 'Grocery & Staples', 'CAT-GROCERY', 'Daily essential food items', 1),
    (1, 'ItemCategory', 'Dairy & Bakery', 'CAT-DAIRY', 'Milk, butter, bread', 2),
    (1, 'ItemCategory', 'Personal Care', 'CAT-CARE', 'Soap, shampoo, toothpaste', 3),
    (1, 'ItemCategory', 'Beverages & Drinks', 'CAT-BEV', 'Tea, coffee, juice', 4),
    (1, 'ItemCategory', 'General & Household', 'CAT-GENERAL', 'Cleaning supplies & utensils', 5);
END

IF NOT EXISTS (SELECT 1 FROM [V_Masters] WHERE [MasterType] = 'UnitOfMeasurement')
BEGIN
    INSERT INTO [V_Masters] ([TenantId], [MasterType], [MasterName], [MasterCode], [Description], [Priority])
    VALUES 
    (1, 'UnitOfMeasurement', 'Kilogram', 'KG', 'kg', 1),
    (1, 'UnitOfMeasurement', 'Gram', 'GM', 'g', 2),
    (1, 'UnitOfMeasurement', 'Liter', 'LTR', 'L', 3),
    (1, 'UnitOfMeasurement', 'Milliliter', 'ML', 'ml', 4),
    (1, 'UnitOfMeasurement', 'Piece', 'PCS', 'pcs', 5),
    (1, 'UnitOfMeasurement', 'Packet', 'PKT', 'pkt', 6),
    (1, 'UnitOfMeasurement', 'Box', 'BOX', 'box', 7),
    (1, 'UnitOfMeasurement', 'Dozen', 'DZN', 'dzn', 8);
END

IF NOT EXISTS (SELECT 1 FROM [V_Masters] WHERE [MasterType] = 'ItemType')
BEGIN
    INSERT INTO [V_Masters] ([TenantId], [MasterType], [MasterName], [MasterCode], [Description], [Priority])
    VALUES 
    (1, 'ItemType', 'Packed Goods', 'PACKED', 'Pre-packaged branded item', 1),
    (1, 'ItemType', 'Loose Goods', 'LOOSE', 'Weighed / loose item', 2),
    (1, 'ItemType', 'Service / Custom', 'SERVICE', 'Non-physical item or custom service', 3);
END

IF NOT EXISTS (SELECT 1 FROM [V_Masters] WHERE [MasterType] = 'Brand')
BEGIN
    INSERT INTO [V_Masters] ([TenantId], [MasterType], [MasterName], [MasterCode], [Description], [Priority])
    VALUES 
    (1, 'Brand', 'General / Local', 'LOCAL', 'Local or unbranded item', 1),
    (1, 'Brand', 'Amul', 'AMUL', 'Amul India', 2),
    (1, 'Brand', 'Tata Consumer', 'TATA', 'Tata Products', 3),
    (1, 'Brand', 'Nestle', 'NESTLE', 'Nestle India', 4);
END

-- Clean up any empty or unnamed master records automatically
UPDATE [V_Masters]
SET [MasterName] = 'Master Item #' + CAST([ID] AS nvarchar(20))
WHERE [MasterName] IS NULL OR LTRIM(RTRIM([MasterName])) = '';";
            db.Database.ExecuteSqlRaw(createMastersSql);

            var ensureCommonColumnsSql = @"
DECLARE @tblName nvarchar(250);
DECLARE tbl_cursor CURSOR FOR SELECT name FROM sys.tables WHERE name LIKE 'V_%';
OPEN tbl_cursor;
FETCH NEXT FROM tbl_cursor INTO @tblName;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC('
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''IsActive'')
            ALTER TABLE [' + @tblName + '] ADD [IsActive] bit NOT NULL DEFAULT 1;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''IsDeleted'')
            ALTER TABLE [' + @tblName + '] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''Priority'')
            ALTER TABLE [' + @tblName + '] ADD [Priority] int NOT NULL DEFAULT 0;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''CreatedBy'')
            ALTER TABLE [' + @tblName + '] ADD [CreatedBy] bigint NOT NULL DEFAULT 0;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''CreatedDate'')
            ALTER TABLE [' + @tblName + '] ADD [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE();
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''ModifiedBy'')
            ALTER TABLE [' + @tblName + '] ADD [ModifiedBy] bigint NOT NULL DEFAULT 0;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''ModifiedDate'')
            ALTER TABLE [' + @tblName + '] ADD [ModifiedDate] datetime2 NOT NULL DEFAULT GETUTCDATE();
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''DeletedBy'')
            ALTER TABLE [' + @tblName + '] ADD [DeletedBy] bigint NOT NULL DEFAULT 0;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''DeletedDate'')
            ALTER TABLE [' + @tblName + '] ADD [DeletedDate] datetime2 NULL;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''EntrySource'')
            ALTER TABLE [' + @tblName + '] ADD [EntrySource] nvarchar(100) NOT NULL DEFAULT '''';
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tblName + ''') AND name = ''IPAddress'')
            ALTER TABLE [' + @tblName + '] ADD [IPAddress] nvarchar(100) NOT NULL DEFAULT '''';
    ');
    FETCH NEXT FROM tbl_cursor INTO @tblName;
END;
CLOSE tbl_cursor;
DEALLOCATE tbl_cursor;";
            db.Database.ExecuteSqlRaw(ensureCommonColumnsSql);
        }
        catch (Exception) {}


        // Ensure ONLY SuperAdmin Default Tenant & System Administrator Account exist in DB
        var superAdminTenant = db.Tenants.FirstOrDefault(t => t.ID == 1 || t.TenantCode == "SUPERADMIN");
        if (superAdminTenant == null)
        {
            superAdminTenant = new Tenant
            {
                TenantCode = "SUPERADMIN",
                TenantName = "SuperAdmin Portal",
                OwnerName = "Super Admin",
                Mobile = "+91 99999 99999",
                Village = "Headquarters",
                IsActive = true,
                IsDeleted = false
            };
            db.Tenants.Add(superAdminTenant);
            try { db.SaveChanges(); } catch (Exception) {}
        }

        var superAdminRole = db.Roles.FirstOrDefault(r => r.TenantId == superAdminTenant.ID);
        if (superAdminRole == null)
        {
            superAdminRole = new Role
            {
                TenantId = superAdminTenant.ID,
                RoleName = "SuperAdmin",
                Description = "System Super Administrator",
                IsSystemRole = true,
                IsActive = true
            };
            db.Roles.Add(superAdminRole);
            try { db.SaveChanges(); } catch (Exception) {}
        }

        var superAdminUser = db.Users.FirstOrDefault(u => u.TenantId == superAdminTenant.ID || u.Username == "superadmin" || u.Username == "admin");
        if (superAdminUser == null)
        {
            db.Users.Add(new User
            {
                TenantId = superAdminTenant.ID,
                Username = "superadmin",
                FullName = "System Super Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345"),
                RoleId = superAdminRole.ID > 0 ? superAdminRole.ID : 1,
                Mobile = "+91 99999 99999",
                IsActive = true,
                IsDeleted = false
            });
            try { db.SaveChanges(); } catch (Exception) {}
        }
        else
        {
            superAdminUser.Username = "superadmin";
            superAdminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345");
            try { db.SaveChanges(); } catch (Exception) {}
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB Auto-Creation Notice: {ex.Message}");
    }
}

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VillageShop API v1");
    c.RoutePrefix = "swagger";
});

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".apk"] = "application/vnd.android.package-archive";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = true
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapGet("/VillageShop.apk", (IWebHostEnvironment env) => ServeApkFile(env));
app.MapGet("/api/app/download", (IWebHostEnvironment env) => ServeApkFile(env));

IResult ServeApkFile(IWebHostEnvironment env)
{
    var searchDirs = new[]
    {
        env.WebRootPath,
        Path.Combine(AppContext.BaseDirectory, "wwwroot"),
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
    };

    foreach (var dir in searchDirs)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

        var exactPath = Path.Combine(dir, "VillageShop.apk");
        if (System.IO.File.Exists(exactPath))
        {
            return Results.File(exactPath, "application/vnd.android.package-archive", "VillageShop.apk");
        }

        var anyApk = Directory.GetFiles(dir, "*.apk", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (anyApk != null)
        {
            return Results.File(anyApk, "application/vnd.android.package-archive", "VillageShop.apk");
        }
    }

    return Results.NotFound("VillageShop APK file is not available on server.");
}

app.MapControllers();

app.MapGet("/", async (HttpContext context) =>
{
    var rootDir = app.Environment.WebRootPath;
    if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir))
    {
        rootDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }
    var filePath = Path.Combine(rootDir, "index.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(filePath);
});

app.MapGet("/admin", async (HttpContext context) =>
{
    var rootDir = app.Environment.WebRootPath;
    if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir))
    {
        rootDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }
    var filePath = Path.Combine(rootDir, "admin", "index.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(filePath);
});

app.MapFallbackToFile("admin/{*path}", "admin/index.html");
app.MapFallbackToFile("index.html");

app.Run();
