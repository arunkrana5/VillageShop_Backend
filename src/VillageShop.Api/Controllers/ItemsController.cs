using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillageShop.Application.Items.DTOs;
using VillageShop.Application.Items.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create([FromBody] CreateItemRequest request)
    {
        if (request == null) request = new CreateItemRequest();
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var response = await _itemService.CreateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PostResponse>> Update(long id, [FromBody] UpdateItemRequest request)
    {
        if (request == null) request = new UpdateItemRequest();
        request.ID = id;
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var response = await _itemService.UpdateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<PostResponse>> Delete(long id)
    {
        var response = await _itemService.DeleteAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Item>> GetById(long id)
    {
        var item = await _itemService.GetByIdAsync(id);
        if (item == null) return NotFound(PostResponse.Error("Item not found.", 404));
        return Ok(item);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Item>>> Search([FromQuery] ItemSearchRequest request)
    {
        if (request == null) request = new ItemSearchRequest();
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var items = await _itemService.SearchAsync(request);
        return Ok(items);
    }
}
