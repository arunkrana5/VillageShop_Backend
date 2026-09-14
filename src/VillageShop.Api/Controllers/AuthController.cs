using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VillageShop.Application.Auth.DTOs;
using VillageShop.Application.Auth.Services;
using VillageShop.Common.Models;

namespace VillageShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<PostResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("register-tenant")]
    public async Task<ActionResult<PostResponse>> RegisterTenant([FromBody] RegisterTenantRequest request)
    {
        var response = await _authService.RegisterTenantAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<PostResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        return StatusCode(response.StatusCode, response);
    }
}
