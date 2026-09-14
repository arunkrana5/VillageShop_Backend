using System.Threading.Tasks;
using VillageShop.Application.Auth.DTOs;
using VillageShop.Common.Models;

namespace VillageShop.Application.Auth.Services;

public interface IAuthService
{
    Task<PostResponse> LoginAsync(LoginRequest request);
    Task<PostResponse> RegisterTenantAsync(RegisterTenantRequest request);
    Task<PostResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
