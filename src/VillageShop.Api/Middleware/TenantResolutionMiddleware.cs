using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VillageShop.Application.Common.Interfaces;

namespace VillageShop.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        var tenantClaim = context.User?.FindFirst("tenant_id")?.Value;
        if (long.TryParse(tenantClaim, out var tenantId) && tenantId > 0)
        {
            tenantService.SetTenantId(tenantId);
        }
        else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerVal) && long.TryParse(headerVal, out var hTenantId) && hTenantId > 0)
        {
            tenantService.SetTenantId(hTenantId);
        }
        else
        {
            tenantService.SetTenantId(1);
        }

        await _next(context);
    }
}
