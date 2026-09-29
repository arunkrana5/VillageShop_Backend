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

        var superAdminUser = db.Users.FirstOrDefault(u => u.TenantId == superAdminTenant.ID || u.Username == "admin");
        if (superAdminUser == null)
        {
            db.Users.Add(new User
            {
                TenantId = superAdminTenant.ID,
                Username = "admin",
                FullName = "Super Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                RoleId = superAdminRole.ID > 0 ? superAdminRole.ID : 1,
                Mobile = "+91 99999 99999",
                IsActive = true,
                IsDeleted = false
            });
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
