using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VillageShop.Api.Middleware;
using VillageShop.Application.Auth.Services;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.Products.Services;
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

// Configure SQL Server DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "villageshop_database.db");
        options.UseSqlite($"Data Source={dbPath}");
    }
});

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IDapperContext, DapperContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Application Services DI
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
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
        db.Database.EnsureCreated();

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

        // Seed initial products if empty
        if (!db.Products.Any(p => !p.IsDeleted))
        {
            db.Products.AddRange(
                new Product { ProductCode = "PRD-001", Name = "Aashirvaad Atta 5kg", Category = "Groceries", Unit = "pkt", PurchasePrice = 195.00m, SellingPrice = 220.00m, MRP = 240.00m, OpeningStock = 15, CurrentStock = 15, TenantId = 1 },
                new Product { ProductCode = "PRD-002", Name = "Fortune Mustard Oil 1L", Category = "Edible Oil", Unit = "bottle", PurchasePrice = 130.00m, SellingPrice = 145.00m, MRP = 160.00m, OpeningStock = 8, CurrentStock = 8, TenantId = 1 },
                new Product { ProductCode = "PRD-003", Name = "Tata Salt 1kg", Category = "Groceries", Unit = "pkt", PurchasePrice = 22.00m, SellingPrice = 28.00m, MRP = 30.00m, OpeningStock = 40, CurrentStock = 40, TenantId = 1 },
                new Product { ProductCode = "PRD-004", Name = "Surf Excel 1kg", Category = "Detergent", Unit = "pkt", PurchasePrice = 110.00m, SellingPrice = 130.00m, MRP = 140.00m, OpeningStock = 12, CurrentStock = 12, TenantId = 1 },
                new Product { ProductCode = "PRD-005", Name = "Sugar (चीनी) 1kg", Category = "Groceries", Unit = "kg", PurchasePrice = 38.00m, SellingPrice = 42.00m, MRP = 45.00m, OpeningStock = 50, CurrentStock = 50, TenantId = 1 }
            );
            db.SaveChanges();
        }

        // Seed initial customers if empty
        if (!db.Customers.Any(c => !c.IsDeleted))
        {
            db.Customers.AddRange(
                new Customer { Name = "Ramesh Kumar", Mobile = "+91 98765 43210", Village = "Rampur", CurrentBalance = 2400.00m, TenantId = 1 },
                new Customer { Name = "Suresh Patel", Mobile = "+91 98123 45678", Village = "Rampur", CurrentBalance = 1200.00m, TenantId = 1 },
                new Customer { Name = "Anita Sharma", Mobile = "+91 97654 32109", Village = "Meerut", CurrentBalance = 0.00m, TenantId = 1 },
                new Customer { Name = "Vikas Verma", Mobile = "+91 99887 76655", Village = "Kisan Nagar", CurrentBalance = 880.00m, TenantId = 1 }
            );
            db.SaveChanges();
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

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = true
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapGet("/VillageShop.apk", (IWebHostEnvironment env) =>
{
    var webRoot = env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
    var apkPath = Path.Combine(webRoot, "VillageShop.apk");
    if (System.IO.File.Exists(apkPath))
    {
        return Results.File(apkPath, "application/vnd.android.package-archive", "VillageShop.apk");
    }
    return Results.NotFound("VillageShop APK file is not available.");
});

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
