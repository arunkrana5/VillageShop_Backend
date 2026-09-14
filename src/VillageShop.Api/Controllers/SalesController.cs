using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.Sales.DTOs;
using VillageShop.Application.Sales.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly IApplicationDbContext _context;

    public SalesController(ISaleService saleService, IApplicationDbContext context)
    {
        _saleService = saleService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSales()
    {
        var sales = await _context.Sales
            .Include(s => s.Customer)
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.ID)
            .Select(s => new
            {
                id = s.InvoiceNumber ?? $"INV-{s.ID}",
                customerName = s.Customer != null ? s.Customer.Name : "Walk-in Customer",
                totalAmount = (double)s.TotalAmount,
                paymentMode = s.PaymentMode ?? "Cash",
                createdAt = s.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                status = s.PaymentMode == "Udhaar" ? "PENDING_CREDIT" : "COMPLETED",
                itemsCount = 1
            })
            .ToListAsync();

        return Ok(sales);
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> CreateSale([FromBody] CreateSaleRequest request)
    {
        var response = await _saleService.CreateSaleAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Sale>> GetSaleById(long id)
    {
        var sale = await _saleService.GetSaleByIdAsync(id);
        if (sale == null) return NotFound(PostResponse.Error("Sale transaction not found.", 404));
        return Ok(sale);
    }
}
