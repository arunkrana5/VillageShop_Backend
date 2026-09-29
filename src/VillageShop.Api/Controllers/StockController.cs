using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillageShop.Application.StockIn.DTOs;
using VillageShop.Application.StockIn.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create([FromBody] CreateStockInRequest request)
    {
        if (request == null) request = new CreateStockInRequest();
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var response = await _stockService.CreateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PostResponse>> Update(long id, [FromBody] UpdateStockInRequest request)
    {
        if (request == null) request = new UpdateStockInRequest();
        request.ID = id;
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var response = await _stockService.UpdateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<PostResponse>> Delete(long id)
    {
        var response = await _stockService.DeleteAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Stock>> GetById(long id)
    {
        var product = await _stockService.GetByIdAsync(id);
        if (product == null) return NotFound(PostResponse.Error("Stock item not found.", 404));
        return Ok(product);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Stock>>> Search([FromQuery] StockInSearchRequest request)
    {
        if (request == null) request = new StockInSearchRequest();
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var products = await _stockService.SearchAsync(request);
        return Ok(products);
    }
}
