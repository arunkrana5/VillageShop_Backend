using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillageShop.Application.ItemMasters.DTOs;
using VillageShop.Application.ItemMasters.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class ItemMastersController : ControllerBase
{
    private readonly IItemMasterService _itemMasterService;

    public ItemMastersController(IItemMasterService itemMasterService)
    {
        _itemMasterService = itemMasterService;
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create([FromBody] CreateItemMasterRequest request)
    {
        if (request == null) request = new CreateItemMasterRequest();
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var response = await _itemMasterService.CreateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PostResponse>> Update(long id, [FromBody] UpdateItemMasterRequest request)
    {
        if (request == null) request = new UpdateItemMasterRequest();
        request.ID = id;
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var response = await _itemMasterService.UpdateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<PostResponse>> Delete(long id)
    {
        var response = await _itemMasterService.DeleteAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemMaster>> GetById(long id)
    {
        var item = await _itemMasterService.GetByIdAsync(id);
        if (item == null) return NotFound(PostResponse.Error("Item Master not found.", 404));
        return Ok(item);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemMaster>>> Search([FromQuery] ItemMasterSearchRequest request)
    {
        if (request == null) request = new ItemMasterSearchRequest();
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }

        var items = await _itemMasterService.SearchAsync(request);
        return Ok(items);
    }
}
