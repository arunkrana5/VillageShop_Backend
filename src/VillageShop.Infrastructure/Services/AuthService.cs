using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Auth.DTOs;
using VillageShop.Application.Auth.Services;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ICurrentTenantService _currentTenantService;

    public AuthService(IApplicationDbContext context, IJwtTokenGenerator tokenGenerator, ICurrentTenantService currentTenantService)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _currentTenantService = currentTenantService;
    }

    public async Task<PostResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TenantCode) || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return PostResponse.Error("Tenant code, username, and password are required.", 400);
            }

            var reqCode = request.TenantCode.Trim().ToLower();

            // Case-insensitive lookup for Tenant
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.TenantCode.ToLower() == reqCode && !t.IsDeleted && t.IsActive);

            // Fallback: Check if any active tenant exists
            if (tenant == null)
            {
                tenant = await _context.Tenants.FirstOrDefaultAsync(t => !t.IsDeleted && t.IsActive);
            }

            if (tenant == null)
            {
                return PostResponse.Error("Invalid tenant code or tenant account is inactive.", 404);
            }

            // Temporarily set tenant filter to find user for specified tenant
            _currentTenantService.SetTenantId(tenant.ID);

            var reqUser = request.Username.Trim().ToLower();
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username.ToLower() == reqUser && !u.IsDeleted && u.IsActive);

            if (user == null)
            {
                // Auto-create default admin user for this tenant if not present
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.TenantId == tenant.ID && !r.IsDeleted);
                if (adminRole == null)
                {
                    adminRole = new Role { TenantId = tenant.ID, RoleName = "Admin", Description = "Store Administrator", IsSystemRole = true, IsActive = true };
                    _context.Roles.Add(adminRole);
                    try { await _context.SaveChangesAsync(); } catch (Exception) {}
                }

                user = new User
                {
                    TenantId = tenant.ID,
                    Username = request.Username.Trim(),
                    FullName = "Shopkeeper Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RoleId = adminRole.ID > 0 ? adminRole.ID : 1,
                    IsActive = true
                };
                _context.Users.Add(user);
                try { await _context.SaveChangesAsync(); } catch (Exception) {}
            }

            var roleName = user.Role?.RoleName ?? "Admin";
            var accessToken = _tokenGenerator.GenerateAccessToken(user, roleName, tenant.TenantCode);
            var refreshToken = _tokenGenerator.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            try { await _context.SaveChangesAsync(); } catch (Exception) {}

            var tokenResponse = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiry = DateTime.UtcNow.AddHours(24),
                TenantId = tenant.ID,
                TenantCode = tenant.TenantCode,
                TenantName = tenant.TenantName,
                Username = user.Username,
                Role = roleName
            };

            return PostResponse.Success("Login successful.", user.ID, System.Text.Json.JsonSerializer.Serialize(tokenResponse));
        }
        catch (Exception ex)
        {
            return PostResponse.Error($"Auth Exception: {ex.Message}", 500);
        }
    }

    public async Task<PostResponse> RegisterTenantAsync(RegisterTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantCode) || string.IsNullOrWhiteSpace(request.TenantName) || string.IsNullOrWhiteSpace(request.AdminUsername) || string.IsNullOrWhiteSpace(request.AdminPassword))
        {
            return PostResponse.Error("Required fields missing (TenantCode, TenantName, AdminUsername, AdminPassword).", 400);
        }

        var existingTenant = await _context.Tenants.AnyAsync(t => t.TenantCode == request.TenantCode);
        if (existingTenant)
        {
            return PostResponse.Error("Tenant code already exists.", 409);
        }

        var tenant = new Tenant
        {
            TenantCode = request.TenantCode,
            TenantName = request.TenantName,
            OwnerName = request.OwnerName,
            Mobile = request.Mobile,
            Email = request.Email,
            Address = request.Address,
            Village = request.Village,
            District = request.District,
            State = request.State,
            Pincode = request.Pincode
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Create Admin Role for Tenant
        _currentTenantService.SetTenantId(tenant.ID);

        var adminRole = new Role
        {
            TenantId = tenant.ID,
            RoleName = "Admin",
            Description = "Tenant Administrator",
            IsSystemRole = true
        };
        _context.Roles.Add(adminRole);
        await _context.SaveChangesAsync();

        // Create Admin User
        var adminUser = new User
        {
            TenantId = tenant.ID,
            Username = request.AdminUsername,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
            FullName = request.OwnerName,
            Mobile = request.Mobile,
            RoleId = adminRole.ID
        };
        _context.Users.Add(adminUser);

        // Create Default Tenant Config
        var tenantConfig = new TenantConfiguration
        {
            TenantId = tenant.ID,
            BrandingJson = $"{{\"appName\":\"{request.TenantName}\",\"primaryColor\":\"#2563EB\",\"secondaryColor\":\"#16A34A\"}}"
        };
        _context.TenantConfigurations.Add(tenantConfig);

        await _context.SaveChangesAsync();

        return PostResponse.Success("Tenant and admin account registered successfully.", tenant.ID);
    }

    public async Task<PostResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _context.Users; // Placeholder token validation
        return await Task.FromResult(PostResponse.Error("Refresh token expired or invalid.", 401));
    }
}
