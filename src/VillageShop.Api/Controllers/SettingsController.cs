using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private static readonly ConcurrentDictionary<long, MobileTenantConfig> _tenantConfigCache = new();
    private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public SettingsController(IApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<long> ResolveTenantIdAsync(string? tenantIdStr, string? tenantCodeStr, long defaultId = 1)
    {
        if (long.TryParse(tenantIdStr, out var tid) && tid > 0) return tid;
        if (!string.IsNullOrWhiteSpace(tenantCodeStr))
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => !t.IsDeleted && (t.TenantCode.ToLower() == tenantCodeStr.ToLower() || t.ID.ToString() == tenantCodeStr));
            if (tenant != null) return tenant.ID;
        }
        return defaultId;
    }

    [HttpGet]
    [HttpGet("mobile-config")]
    [HttpGet("config")]
    public async Task<IActionResult> GetMobileConfig([FromQuery] long? tenantId, [FromQuery] string? tenantCode, [FromQuery] string? code)
    {
        long targetTenantId = tenantId ?? await ResolveTenantIdAsync(null, tenantCode ?? code, 0);

        if (targetTenantId <= 0 && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            targetTenantId = headerTid;
        }

        if (targetTenantId <= 0 && Request.Headers.TryGetValue("X-Tenant-Code", out var headerTCode) && !string.IsNullOrWhiteSpace(headerTCode))
        {
            targetTenantId = await ResolveTenantIdAsync(null, headerTCode, 0);
        }

        if (targetTenantId <= 0 && Request.Headers.TryGetValue("Authorization", out var authHeader) && authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var tokenStr = authHeader.ToString().Substring(7).Trim();
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                if (handler.CanReadToken(tokenStr))
                {
                    var jwt = handler.ReadJwtToken(tokenStr);
                    var tidClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
                    var tcodeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_code")?.Value;
                    if (long.TryParse(tidClaim, out var parsedTid) && parsedTid > 0)
                    {
                        targetTenantId = parsedTid;
                    }
                    else if (!string.IsNullOrWhiteSpace(tcodeClaim))
                    {
                        targetTenantId = await ResolveTenantIdAsync(null, tcodeClaim, 0);
                    }
                }
            }
            catch (Exception) {}
        }

        if (targetTenantId <= 0) targetTenantId = 1;

        var config = await GetOrLoadTenantConfigAsync(targetTenantId);
        var jsonConfig = JsonSerializer.Serialize(config, JsonOpts);

        return Ok(new PostResponse
        {
            Status = true,
            StatusCode = 200,
            Message = $"Mobile app configuration for Tenant ID {targetTenantId} fetched successfully.",
            ID = (int)targetTenantId,
            AdditionalMessage = jsonConfig
        });
    }

    [HttpPost]
    [HttpPut]
    [HttpPost("mobile-config")]
    [HttpPost("config")]
    [HttpPut("mobile-config")]
    [HttpPut("config")]
    public async Task<IActionResult> UpdateMobileConfig([FromBody] MobileTenantConfig newConfig, [FromQuery] long? tenantId, [FromQuery] string? tenantCode)
    {
        if (newConfig == null) newConfig = new MobileTenantConfig();

        long resolvedId = tenantId ?? newConfig.SelectedTenantId ?? await ResolveTenantIdAsync(null, tenantCode ?? newConfig.TenantCode);
        long targetTenantId = resolvedId > 0 ? resolvedId : 1;
        newConfig.SelectedTenantId = targetTenantId;

        if (!string.IsNullOrWhiteSpace(newConfig.AppName))
        {
            newConfig.TenantName = newConfig.AppName;
            newConfig.AppTitle = newConfig.AppName;
        }
        else if (!string.IsNullOrWhiteSpace(newConfig.TenantName))
        {
            newConfig.AppName = newConfig.TenantName;
        }

        if (!string.IsNullOrWhiteSpace(newConfig.PrimaryColor)) newConfig.PrimaryColorHex = newConfig.PrimaryColor;
        else newConfig.PrimaryColor = newConfig.PrimaryColorHex;

        if (!string.IsNullOrWhiteSpace(newConfig.SecondaryColor)) newConfig.SecondaryColorHex = newConfig.SecondaryColor;
        else newConfig.SecondaryColor = newConfig.SecondaryColorHex;

        if (!string.IsNullOrWhiteSpace(newConfig.TextColor)) newConfig.TextColorHex = newConfig.TextColor;
        else newConfig.TextColor = newConfig.TextColorHex;

        if (!string.IsNullOrWhiteSpace(newConfig.PageBgColor)) newConfig.PageBgColorHex = newConfig.PageBgColor;
        else newConfig.PageBgColor = newConfig.PageBgColorHex;

        if (!string.IsNullOrWhiteSpace(newConfig.CardBgColor)) newConfig.CardBgColorHex = newConfig.CardBgColor;
        else newConfig.CardBgColor = newConfig.CardBgColorHex;

        if (!string.IsNullOrWhiteSpace(newConfig.AmountColor)) newConfig.AmountColorHex = newConfig.AmountColor;
        else newConfig.AmountColor = newConfig.AmountColorHex;

        if (!string.IsNullOrWhiteSpace(newConfig.ButtonBgColor)) newConfig.ButtonBgColorHex = newConfig.ButtonBgColor;
        else newConfig.ButtonBgColor = newConfig.ButtonBgColorHex;

        if (!string.IsNullOrWhiteSpace(newConfig.ButtonTextColor)) newConfig.ButtonTextColorHex = newConfig.ButtonTextColor;
        else newConfig.ButtonTextColor = newConfig.ButtonTextColorHex;

        var defaults = await GetInitialDefaultConfigForTenantAsync(targetTenantId);
        if (newConfig.Tenants == null || newConfig.Tenants.Count == 0) newConfig.Tenants = defaults.Tenants;
        if (newConfig.MenuItems == null || newConfig.MenuItems.Count == 0) newConfig.MenuItems = defaults.MenuItems;

        _tenantConfigCache[targetTenantId] = newConfig;

        try
        {
            var existingTenant = await _context.Tenants.FirstOrDefaultAsync(t => t.ID == targetTenantId && !t.IsDeleted);
            if (existingTenant == null)
            {
                var codeName = $"TNT_{targetTenantId:D3}";
                existingTenant = new Tenant
                {
                    TenantCode = codeName,
                    TenantName = newConfig.TenantName,
                    OwnerName = "SaaS Owner",
                    Mobile = newConfig.SupportPhone
                };
                _context.Tenants.Add(existingTenant);
            }
            else
            {
                existingTenant.TenantName = newConfig.TenantName;
                if (!string.IsNullOrWhiteSpace(newConfig.SupportPhone)) existingTenant.Mobile = newConfig.SupportPhone;
            }

            var brandingObj = new
            {
                appName = newConfig.TenantName,
                primaryColor = newConfig.PrimaryColorHex,
                secondaryColor = newConfig.SecondaryColorHex,
                tenantName = newConfig.TenantName,
                appTitle = newConfig.AppTitle,
                logoUrl = newConfig.LogoUrl,
                logoIcon = newConfig.LogoIcon,
                primaryColorHex = newConfig.PrimaryColorHex,
                secondaryColorHex = newConfig.SecondaryColorHex,
                accentColorHex = newConfig.AccentColorHex,
                tagline = newConfig.Tagline,
                currencySymbol = newConfig.CurrencySymbol,
                fontFamily = newConfig.FontFamily,
                fontSizeScale = newConfig.FontSizeScale,
                textColorHex = newConfig.TextColorHex,
                textColor = newConfig.TextColorHex,
                pageBgColorHex = newConfig.PageBgColorHex,
                pageBgColor = newConfig.PageBgColorHex,
                cardBgColorHex = newConfig.CardBgColorHex,
                cardBgColor = newConfig.CardBgColorHex,
                amountColorHex = newConfig.AmountColorHex,
                amountColor = newConfig.AmountColorHex,
                buttonBgColorHex = newConfig.ButtonBgColorHex,
                buttonBgColor = newConfig.ButtonBgColorHex,
                buttonTextColorHex = newConfig.ButtonTextColorHex,
                buttonTextColor = newConfig.ButtonTextColorHex,
                supportPhone = newConfig.SupportPhone,
                supportEmail = newConfig.SupportEmail,
                supportWhatsapp = newConfig.SupportWhatsapp,
                supportHours = newConfig.SupportHours
            };

            var featureObj = new
            {
                newConfig.EnableUdhaar,
                newConfig.EnableBarcodeScanner,
                newConfig.EnableOnlinePayment,
                newConfig.EnableHindiLanguage,
                newConfig.EnableReceiptPrinting,
                newConfig.EnablePOSDiscount,
                newConfig.EnableTaxCalculation,
                newConfig.DefaultTaxPercent,
                newConfig.AllowNegativeStock,
                newConfig.LowStockThreshold
            };

            var brandingJsonStr = JsonSerializer.Serialize(brandingObj, JsonOpts);
            var featureJsonStr = JsonSerializer.Serialize(featureObj, JsonOpts);
            var menuJsonStr = JsonSerializer.Serialize(newConfig.MenuItems, JsonOpts);

            var existingConfig = await _context.TenantConfigurations.FirstOrDefaultAsync(c => c.TenantId == targetTenantId && !c.IsDeleted);
            if (existingConfig == null)
            {
                existingConfig = new TenantConfiguration
                {
                    TenantId = targetTenantId,
                    BrandingJson = brandingJsonStr,
                    FeatureJson = featureJsonStr,
                    MenuJson = menuJsonStr
                };
                _context.TenantConfigurations.Add(existingConfig);
            }
            else
            {
                existingConfig.BrandingJson = brandingJsonStr;
                existingConfig.FeatureJson = featureJsonStr;
                existingConfig.MenuJson = menuJsonStr;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception) {}

        var jsonConfig = JsonSerializer.Serialize(newConfig, JsonOpts);
        return Ok(new PostResponse
        {
            Status = true,
            StatusCode = 200,
            Message = $"Configuration published & saved for Tenant ID {targetTenantId} ({newConfig.TenantName}) successfully!",
            ID = (int)targetTenantId,
            AdditionalMessage = jsonConfig
        });
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
    {
        var dbTenants = await _context.Tenants.IgnoreQueryFilters().Where(t => !t.IsDeleted).ToListAsync();
        var tenants = dbTenants.Select(t => new TenantInfo
        {
            Id = t.ID,
            TenantId = $"TNT-{t.ID:D3}",
            Name = t.TenantName,
            Code = t.TenantCode,
            Plan = "Enterprise SaaS",
            ActiveStatus = t.IsActive ? "ACTIVE" : "INACTIVE",
            OwnerName = t.OwnerName ?? "Store Owner",
            OwnerPhone = t.Mobile ?? "+91 98765 43210",
            JoinedDate = t.CreatedDate.ToString("dd MMM yyyy"),
            StoresCount = 1
        }).ToList();

        return Ok(new PostResponse
        {
            Status = true,
            StatusCode = 200,
            Message = "Registered SaaS clients list retrieved from DB successfully.",
            AdditionalMessage = JsonSerializer.Serialize(tenants, JsonOpts)
        });
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new PostResponse
            {
                Status = false,
                StatusCode = 400,
                Message = "Tenant code and name are required."
            });
        }

        var cleanCode = req.Code.Trim().ToUpper();
        var existing = await _context.Tenants.FirstOrDefaultAsync(t => t.TenantCode == cleanCode && !t.IsDeleted);
        if (existing != null)
        {
            return BadRequest(new PostResponse
            {
                Status = false,
                StatusCode = 400,
                Message = $"Tenant code '{cleanCode}' already exists."
            });
        }

        var tenant = new Tenant
        {
            TenantCode = cleanCode,
            TenantName = req.Name.Trim(),
            OwnerName = string.IsNullOrWhiteSpace(req.OwnerName) ? "Store Owner" : req.OwnerName.Trim(),
            Mobile = string.IsNullOrWhiteSpace(req.OwnerPhone) ? "+91 98765 43210" : req.OwnerPhone.Trim(),
            Email = req.Email,
            Village = req.Village ?? "Rampur",
            IsActive = req.ActiveStatus != "SUSPENDED" && req.ActiveStatus != "INACTIVE"
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        try
        {
            var role = new Role
            {
                TenantId = tenant.ID,
                RoleName = "Admin",
                Description = "Store Administrator",
                IsSystemRole = true,
                IsActive = true
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var passHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            var user = new User
            {
                TenantId = tenant.ID,
                Username = "admin",
                FullName = tenant.OwnerName,
                Mobile = tenant.Mobile,
                Email = tenant.Email,
                PasswordHash = passHash,
                RoleId = role.ID,
                IsActive = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await CreateAndSeedDefaultTenantConfigInDbAsync(tenant.ID);
        }
        catch (Exception) {}

        return Ok(new PostResponse
        {
            Status = true,
            StatusCode = 200,
            Message = $"Client store tenant '{tenant.TenantName}' ({tenant.TenantCode}) created successfully.",
            ID = (int)tenant.ID
        });
    }

    [HttpPut("tenants/{id}")]
    public async Task<IActionResult> UpdateTenant(long id, [FromBody] CreateTenantRequest req)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.ID == id && !t.IsDeleted);
        if (tenant == null)
        {
            return NotFound(new PostResponse { Status = false, StatusCode = 404, Message = "Tenant not found." });
        }

        if (!string.IsNullOrWhiteSpace(req.Name)) tenant.TenantName = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.OwnerName)) tenant.OwnerName = req.OwnerName.Trim();
        if (!string.IsNullOrWhiteSpace(req.OwnerPhone)) tenant.Mobile = req.OwnerPhone.Trim();
        if (req.ActiveStatus != null) tenant.IsActive = req.ActiveStatus == "ACTIVE";

        await _context.SaveChangesAsync();

        return Ok(new PostResponse
        {
            Status = true,
            StatusCode = 200,
            Message = $"Tenant '{tenant.TenantName}' updated successfully."
        });
    }

    [HttpDelete("tenants/{id}")]
    public async Task<IActionResult> DeleteTenant(long id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.ID == id);
        if (tenant != null)
        {
            tenant.IsDeleted = true;
            tenant.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return Ok(new PostResponse
        {
            Status = true,
            StatusCode = 200,
            Message = "Tenant deleted successfully."
        });
    }

    private async Task<MobileTenantConfig> GetOrLoadTenantConfigAsync(long tenantId)
    {
        if (_tenantConfigCache.TryGetValue(tenantId, out var cached)) return cached;

        try
        {
            var dbConfig = await _context.TenantConfigurations.FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);
            if (dbConfig != null && !string.IsNullOrWhiteSpace(dbConfig.BrandingJson))
            {
                var loadedBranding = JsonSerializer.Deserialize<MobileTenantConfig>(dbConfig.BrandingJson, JsonOpts)
                    ?? await CreateAndSeedDefaultTenantConfigInDbAsync(tenantId);

                if (!string.IsNullOrWhiteSpace(dbConfig.MenuJson))
                {
                    var loadedMenu = JsonSerializer.Deserialize<List<MenuItemConfig>>(dbConfig.MenuJson, JsonOpts);
                    if (loadedMenu != null && loadedMenu.Count > 0) loadedBranding.MenuItems = loadedMenu;
                }

                _tenantConfigCache[tenantId] = loadedBranding;
                return loadedBranding;
            }
        }
        catch (Exception) {}

        var initialAndPersisted = await CreateAndSeedDefaultTenantConfigInDbAsync(tenantId);
        _tenantConfigCache[tenantId] = initialAndPersisted;
        return initialAndPersisted;
    }

    private async Task<MobileTenantConfig> CreateAndSeedDefaultTenantConfigInDbAsync(long tenantId)
    {
        var defaultConfig = await GetInitialDefaultConfigForTenantAsync(tenantId);

        var brandingObj = new
        {
            appName = defaultConfig.TenantName,
            primaryColor = defaultConfig.PrimaryColorHex,
            secondaryColor = defaultConfig.SecondaryColorHex,
            tenantName = defaultConfig.TenantName,
            appTitle = defaultConfig.AppTitle,
            logoUrl = defaultConfig.LogoUrl,
            logoIcon = defaultConfig.LogoIcon,
            primaryColorHex = defaultConfig.PrimaryColorHex,
            secondaryColorHex = defaultConfig.SecondaryColorHex,
            accentColorHex = defaultConfig.AccentColorHex,
            tagline = defaultConfig.Tagline,
            currencySymbol = defaultConfig.CurrencySymbol,
            fontFamily = defaultConfig.FontFamily,
            fontSizeScale = defaultConfig.FontSizeScale,
            textColorHex = defaultConfig.TextColorHex,
            textColor = defaultConfig.TextColorHex,
            pageBgColorHex = defaultConfig.PageBgColorHex,
            pageBgColor = defaultConfig.PageBgColorHex,
            cardBgColorHex = defaultConfig.CardBgColorHex,
            cardBgColor = defaultConfig.CardBgColorHex,
            amountColorHex = defaultConfig.AmountColorHex,
            amountColor = defaultConfig.AmountColorHex,
            buttonBgColorHex = defaultConfig.ButtonBgColorHex,
            buttonBgColor = defaultConfig.ButtonBgColorHex,
            buttonTextColorHex = defaultConfig.ButtonTextColorHex,
            buttonTextColor = defaultConfig.ButtonTextColorHex
        };

        var featureObj = new
        {
            defaultConfig.EnableUdhaar,
            defaultConfig.EnableBarcodeScanner,
            defaultConfig.EnableOnlinePayment,
            defaultConfig.EnableHindiLanguage,
            defaultConfig.EnableReceiptPrinting,
            defaultConfig.EnablePOSDiscount,
            defaultConfig.EnableTaxCalculation,
            defaultConfig.DefaultTaxPercent,
            defaultConfig.AllowNegativeStock,
            defaultConfig.LowStockThreshold,
            defaultConfig.SupportPhone,
            defaultConfig.SupportEmail,
            defaultConfig.SupportWhatsapp,
            defaultConfig.SupportHours
        };

        var brandingJsonStr = JsonSerializer.Serialize(brandingObj, JsonOpts);
        var featureJsonStr = JsonSerializer.Serialize(featureObj, JsonOpts);
        var menuJsonStr = JsonSerializer.Serialize(defaultConfig.MenuItems, JsonOpts);

        try
        {
            var newDbRecord = new TenantConfiguration
            {
                TenantId = tenantId,
                BrandingJson = brandingJsonStr,
                FeatureJson = featureJsonStr,
                MenuJson = menuJsonStr
            };
            _context.TenantConfigurations.Add(newDbRecord);
            await _context.SaveChangesAsync();
        }
        catch (Exception) {}

        return defaultConfig;
    }

    private async Task<MobileTenantConfig> GetInitialDefaultConfigForTenantAsync(long tenantId)
    {
        var tenantDbRecord = await _context.Tenants.FirstOrDefaultAsync(t => t.ID == tenantId && !t.IsDeleted);
        var tName = !string.IsNullOrWhiteSpace(tenantDbRecord?.TenantName) ? tenantDbRecord.TenantName : $"Store Client #{tenantId}";
        var tCode = !string.IsNullOrWhiteSpace(tenantDbRecord?.TenantCode) ? tenantDbRecord.TenantCode : $"TNT_{tenantId}";
        var tMobile = !string.IsNullOrWhiteSpace(tenantDbRecord?.Mobile) ? tenantDbRecord.Mobile : "+91 98765 43210";
        var tOwner = !string.IsNullOrWhiteSpace(tenantDbRecord?.OwnerName) ? tenantDbRecord.OwnerName : "Store Owner";

        var config = new MobileTenantConfig
        {
            SelectedTenantId = tenantId,
            TenantName = tName,
            TenantCode = tCode,
            AppName = tName,
            AppTitle = tName,
            PrimaryColorHex = "#0F172A",
            SecondaryColorHex = "#D97706",
            ButtonBgColorHex = "#2563EB",
            Tagline = "Digital Store System",
            CurrencySymbol = "₹",
            FontFamily = "Roboto",
            FontSizeScale = 1.0,
            EnableUdhaar = true,
            EnableBarcodeScanner = true,
            EnableOnlinePayment = true,
            EnableHindiLanguage = true,
            EnableReceiptPrinting = true,
            EnablePOSDiscount = true,
            EnableTaxCalculation = true,
            DefaultTaxPercent = 5.0,
            AllowNegativeStock = false,
            LowStockThreshold = 5,
            SupportPhone = tMobile,
            SupportEmail = !string.IsNullOrWhiteSpace(tenantDbRecord?.TenantCode) ? $"support@{tenantDbRecord.TenantCode.ToLower()}.com" : "support@store.com",
            SupportWhatsapp = tMobile,
            SupportHours = "9:00 AM - 9:00 PM",
            OwnerName = tOwner,
            OwnerPhone = tMobile,
            Plan = "Enterprise SaaS",
            ActiveStatus = "ACTIVE",
            StoresCount = 1,
            MenuItems = new List<MenuItemConfig>
            {
                new MenuItemConfig { Id = "dashboard", TitleEn = "Dashboard", TitleHi = "डैशबोर्ड", Icon = "dashboard_rounded", Route = "/home", IsEnabled = true },
                new MenuItemConfig { Id = "pos", TitleEn = "New Sale / POS", TitleHi = "नया बिल / POS", Icon = "point_of_sale_rounded", Route = "/pos", IsEnabled = true, BadgeText = "FAST" },
                new MenuItemConfig { Id = "products", TitleEn = "Products & Stock", TitleHi = "सामान और स्टॉक", Icon = "inventory_2_rounded", Route = "/products", IsEnabled = true },
                new MenuItemConfig { Id = "customers", TitleEn = "Customer Udhaar", TitleHi = "ग्राहक उधार खाता", Icon = "people_alt_rounded", Route = "/customers", IsEnabled = true },
                new MenuItemConfig { Id = "reports", TitleEn = "Reports & Earnings", TitleHi = "रिपोर्ट और कमाई", Icon = "analytics_rounded", Route = "/reports", IsEnabled = true },
                new MenuItemConfig { Id = "settings", TitleEn = "Settings", TitleHi = "सेटिंग्स", Icon = "settings_rounded", Route = "/settings", IsEnabled = true },
            }
        };

        config.PrimaryColor = config.PrimaryColorHex;
        config.SecondaryColor = config.SecondaryColorHex;
        return config;
    }
}

public class MenuItemConfig
{
    public string Id { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string TitleHi { get; set; } = "";
    public string Icon { get; set; } = "dashboard_rounded";
    public string Route { get; set; } = "/home";
    public bool IsEnabled { get; set; } = true;
    public string BadgeText { get; set; } = "";
}

public class TenantInfo
{
    public long Id { get; set; }
    public string TenantId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Plan { get; set; } = "";
    public string ActiveStatus { get; set; } = "ACTIVE";
    public string OwnerName { get; set; } = "";
    public string OwnerPhone { get; set; } = "";
    public string JoinedDate { get; set; } = "";
    public int StoresCount { get; set; } = 1;
}

public class MobileTenantConfig
{
    public long? SelectedTenantId { get; set; } = 1;
    public string TenantCode { get; set; } = "";
    public string TenantName { get; set; } = "";
    public string AppName { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string LogoIcon { get; set; } = "storefront_rounded";
    public string PrimaryColorHex { get; set; } = "#0F172A";
    public string PrimaryColor { get; set; } = "";
    public string SecondaryColorHex { get; set; } = "#D97706";
    public string SecondaryColor { get; set; } = "";
    public string AccentColorHex { get; set; } = "#10B981";
    public string AppTitle { get; set; } = "";
    public string Tagline { get; set; } = "Digital Store System";
    public string CurrencySymbol { get; set; } = "₹";
    public string FontFamily { get; set; } = "Roboto";
    public double FontSizeScale { get; set; } = 1.0;

    public string TextColorHex { get; set; } = "#0F172A";
    public string TextColor { get; set; } = "";
    public string PageBgColorHex { get; set; } = "#F8FAFC";
    public string PageBgColor { get; set; } = "";
    public string CardBgColorHex { get; set; } = "#FFFFFF";
    public string CardBgColor { get; set; } = "";
    public string AmountColorHex { get; set; } = "#16A34A";
    public string AmountColor { get; set; } = "";
    public string ButtonBgColorHex { get; set; } = "#2563EB";
    public string ButtonBgColor { get; set; } = "";
    public string ButtonTextColorHex { get; set; } = "#FFFFFF";
    public string ButtonTextColor { get; set; } = "";

    // 360-Degree Feature Controls
    public bool EnableUdhaar { get; set; } = true;
    public bool EnableBarcodeScanner { get; set; } = true;
    public bool EnableOnlinePayment { get; set; } = true;
    public bool EnableHindiLanguage { get; set; } = true;
    public bool EnableReceiptPrinting { get; set; } = true;
    public bool EnablePOSDiscount { get; set; } = true;
    public bool EnableTaxCalculation { get; set; } = true;
    public double DefaultTaxPercent { get; set; } = 5.0;
    public bool AllowNegativeStock { get; set; } = false;
    public int LowStockThreshold { get; set; } = 5;

    // Customer Support Contact
    public string SupportPhone { get; set; } = "";
    public string SupportEmail { get; set; } = "";
    public string SupportWhatsapp { get; set; } = "";
    public string SupportHours { get; set; } = "9:00 AM - 9:00 PM";

    public string OwnerName { get; set; } = "";
    public string OwnerPhone { get; set; } = "";
    public string Plan { get; set; } = "Enterprise SaaS";
    public string ActiveStatus { get; set; } = "ACTIVE";
    public int StoresCount { get; set; } = 1;

    public List<MenuItemConfig> MenuItems { get; set; } = new List<MenuItemConfig>();
    public List<TenantInfo> Tenants { get; set; } = new List<TenantInfo>();
}

public class CreateTenantRequest
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string OwnerPhone { get; set; } = "";
    public string? Email { get; set; }
    public string? Village { get; set; }
    public string Plan { get; set; } = "Enterprise SaaS";
    public string ActiveStatus { get; set; } = "ACTIVE";
}
