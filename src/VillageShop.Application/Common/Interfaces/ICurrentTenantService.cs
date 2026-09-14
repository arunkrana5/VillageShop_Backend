namespace VillageShop.Application.Common.Interfaces;

public interface ICurrentTenantService
{
    long TenantId { get; }
    long UserId { get; }
    string Username { get; }
    string RoleName { get; }
    bool IsSuperAdmin { get; }
    void SetTenantId(long tenantId);
}
