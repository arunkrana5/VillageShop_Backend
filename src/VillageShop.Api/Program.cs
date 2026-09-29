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
        try
        {
            db.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Items')
                CREATE TABLE V_Items (
                    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
                    TenantId BIGINT NOT NULL,
                    ItemCode NVARCHAR(100) NULL,
                    Name NVARCHAR(255) NOT NULL,
                    Category NVARCHAR(100) NULL,
                    Unit NVARCHAR(50) NULL,
                    Format NVARCHAR(50) NULL,
                    Description NVARCHAR(MAX) NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    IsDeleted BIT NOT NULL DEFAULT 0,
                    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    CreatedBy BIGINT NULL,
                    ModifiedDate DATETIME2 NULL,
                    ModifiedBy BIGINT NULL,
                    DeletedDate DATETIME2 NULL,
                    DeletedBy BIGINT NULL,
                    IPAddress NVARCHAR(100) NULL,
                    EntrySource NVARCHAR(100) NULL,
                    Priority INT NOT NULL DEFAULT 0
                );

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Stock')
                CREATE TABLE V_Stock (
                    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
                    TenantId BIGINT NOT NULL,
                    ItemId BIGINT NULL,
                    ProductCode NVARCHAR(100) NULL,
                    Name NVARCHAR(255) NOT NULL,
                    Category NVARCHAR(100) NULL,
                    Brand NVARCHAR(100) NULL,
                    Unit NVARCHAR(50) NULL,
                    Barcode NVARCHAR(100) NULL,
                    PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                    SellingPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                    MRP DECIMAL(18,2) NOT NULL DEFAULT 0,
                    GSTPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
                    OpeningStock DECIMAL(18,3) NOT NULL DEFAULT 0,
                    MinimumStock DECIMAL(18,3) NOT NULL DEFAULT 5,
                    CurrentStock DECIMAL(18,3) NOT NULL DEFAULT 0,
                    BatchNumber NVARCHAR(100) NULL,
                    RackNumber NVARCHAR(100) NULL,
                    ExpiryDate DATETIME2 NULL,
                    HSNCode NVARCHAR(50) NULL,
                    ImageUrl NVARCHAR(MAX) NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    IsDeleted BIT NOT NULL DEFAULT 0,
                    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    CreatedBy BIGINT NULL,
                    ModifiedDate DATETIME2 NULL,
                    ModifiedBy BIGINT NULL,
                    DeletedDate DATETIME2 NULL,
                    DeletedBy BIGINT NULL,
                    IPAddress NVARCHAR(100) NULL,
                    EntrySource NVARCHAR(100) NULL,
                    Priority INT NOT NULL DEFAULT 0
                );
            ");
        } catch (Exception) {}
        try { db.Database.ExecuteSqlRaw("IF EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Products') AND NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V_Stock') EXEC sp_rename 'V_Products', 'V_Stock';"); } catch (Exception) {}
        try { db.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Stock') AND name = 'ImageUrl') ALTER TABLE V_Stock ADD ImageUrl NVARCHAR(MAX) NULL;"); } catch (Exception) {}
        try { db.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Stock') AND name = 'ItemId') ALTER TABLE V_Stock ADD ItemId BIGINT NULL;"); } catch (Exception) {}
        try { db.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Stock') AND name = 'BatchNumber') ALTER TABLE V_Stock ADD BatchNumber NVARCHAR(100) NULL;"); } catch (Exception) {}
        try { db.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Stock') AND name = 'RackNumber') ALTER TABLE V_Stock ADD RackNumber NVARCHAR(100) NULL;"); } catch (Exception) {}
        try { db.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Stock') AND name = 'ExpiryDate') ALTER TABLE V_Stock ADD ExpiryDate DATETIME2 NULL;"); } catch (Exception) {}
        try { db.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('V_Stock') AND name = 'HSNCode') ALTER TABLE V_Stock ADD HSNCode NVARCHAR(50) NULL;"); } catch (Exception) {}


        // Ensure All 4 SaaS Client Tenants & Dedicated User Accounts exist in DB
        var targetClients = new[]
        {
            new { Id = 1L, Code = "SHARMA_SHOP", Name = "Sharma General Store", Owner = "Rajesh Sharma", Phone = "+91 98765 43210", User = "admin", Pass = "admin123" },
            new { Id = 2L, Code = "GUPTA_KIRANA", Name = "Gupta Kirana & Provisions", Owner = "Suresh Gupta", Phone = "+91 98123 45678", User = "gupta_admin", Pass = "gupta123" },
            new { Id = 3L, Code = "VERMA_TRADERS", Name = "Verma Traders & Seeds", Owner = "Vikas Verma", Phone = "+91 97654 32109", User = "verma_admin", Pass = "verma123" },
            new { Id = 4L, Code = "KISAN_AGRO", Name = "Kisan Agro Store", Owner = "Ramesh Kisan", Phone = "+91 99000 11223", User = "kisan_admin", Pass = "kisan123" }
        };

        foreach (var c in targetClients)
        {
            var existingTenant = db.Tenants.FirstOrDefault(t => t.ID == c.Id || t.TenantCode == c.Code);
            if (existingTenant == null)
            {
                existingTenant = new Tenant
                {
                    TenantCode = c.Code,
                    TenantName = c.Name,
                    OwnerName = c.Owner,
                    Mobile = c.Phone,
                    Village = "Rampur",
                    IsActive = true,
                    IsDeleted = false
                };
                db.Tenants.Add(existingTenant);
                try { db.SaveChanges(); } catch (Exception) {}
            }
            else
            {
                existingTenant.TenantName = c.Name;
                existingTenant.TenantCode = c.Code;
                existingTenant.OwnerName = c.Owner;
                if (!string.IsNullOrWhiteSpace(c.Phone)) existingTenant.Mobile = c.Phone;
                try { db.SaveChanges(); } catch (Exception) {}
            }

            var targetTenantId = existingTenant.ID > 0 ? existingTenant.ID : c.Id;

            var existingRole = db.Roles.FirstOrDefault(r => r.TenantId == targetTenantId);
            if (existingRole == null)
            {
                existingRole = new Role
                {
                    TenantId = targetTenantId,
                    RoleName = "Admin",
                    Description = "Store Administrator",
                    IsSystemRole = true,
                    IsActive = true
                };
                db.Roles.Add(existingRole);
                try { db.SaveChanges(); } catch (Exception) {}
            }

            var existingUser = db.Users.FirstOrDefault(u => u.TenantId == targetTenantId || u.Username == c.User);
            if (existingUser == null)
            {
                db.Users.Add(new User
                {
                    TenantId = targetTenantId,
                    Username = c.User,
                    FullName = $"{c.Owner} ({c.Name})",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(c.Pass),
                    RoleId = existingRole.ID > 0 ? existingRole.ID : 1,
                    Mobile = c.Phone,
                    IsActive = true,
                    IsDeleted = false
                });
                try { db.SaveChanges(); } catch (Exception) {}
            }
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
