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
[Route("api/stock")]
[Route("api/stockin")]
[Route("api/stock-in")]
[Route("api/products")]
public class StockController : ControllerBase
{
    private readonly IStockInService _stockInService;

    public StockController(IStockInService stockInService)
    {
        _stockInService = stockInService;
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create([FromBody] CreateStockInRequest request)
    {
        if (request == null) request = new CreateStockInRequest();
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var response = await _stockInService.CreateAsync(request);
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

        var response = await _stockInService.UpdateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<PostResponse>> Delete(long id)
    {
        var response = await _stockInService.DeleteAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Stock>> GetById(long id)
    {
        var product = await _stockInService.GetByIdAsync(id);
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

        var products = await _stockInService.SearchAsync(request);
        return Ok(products);
    }
}
