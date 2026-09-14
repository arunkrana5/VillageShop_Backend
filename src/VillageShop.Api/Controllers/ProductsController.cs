using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillageShop.Application.Products.DTOs;
using VillageShop.Application.Products.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create([FromBody] CreateProductRequest request)
    {
        var response = await _productService.CreateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PostResponse>> Update(long id, [FromBody] UpdateProductRequest request)
    {
        request.ID = id;
        var response = await _productService.UpdateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<PostResponse>> Delete(long id)
    {
        var response = await _productService.DeleteAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(long id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound(PostResponse.Error("Product not found.", 404));
        return Ok(product);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> Search([FromQuery] ProductSearchRequest request)
    {
        if (request.PageNumber <= 0) request.PageNumber = 1;
        if (request.PageSize <= 0) request.PageSize = 200;
        var products = await _productService.SearchAsync(request);
        return Ok(products);
    }
}
