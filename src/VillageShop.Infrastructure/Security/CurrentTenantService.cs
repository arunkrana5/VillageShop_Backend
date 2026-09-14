using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VillageShop.Application.Common.Interfaces;

namespace VillageShop.Infrastructure.Security;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private long? _overrideTenantId;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long TenantId
    {
        get
        {
            if (_overrideTenantId.HasValue) return _overrideTenantId.Value;

            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            if (long.TryParse(tenantClaim, out var tenantId))
            {
                return tenantId;
            }
            return 1;
        }
    }

    public long UserId
    {
        get
        {
            var userClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userClaim, out var userId))
            {
                return userId;
            }
            return 0;
        }
    }

    public string Username => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    public string RoleName => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    public bool IsSuperAdmin => RoleName.Equals("SuperAdmin", System.StringComparison.OrdinalIgnoreCase);

    public void SetTenantId(long tenantId)
    {
        _overrideTenantId = tenantId;
    }
}
