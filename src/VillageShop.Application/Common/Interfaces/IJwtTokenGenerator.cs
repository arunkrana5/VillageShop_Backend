using VillageShop.Domain.Entities;

namespace VillageShop.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, string roleName, string tenantCode);
    string GenerateRefreshToken();
}
