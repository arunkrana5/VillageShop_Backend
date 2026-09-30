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
    public async Task<IActionResult> GetSales([FromQuery] long? tenantId, [FromQuery] string? tenantCode)
    {
        long targetTenantId = tenantId ?? 0;
        if (targetTenantId <= 0 && !string.IsNullOrWhiteSpace(tenantCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == tenantCode.ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }

        if (targetTenantId <= 0 && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            targetTenantId = headerTid;
        }

        if (targetTenantId <= 0 && Request.Headers.TryGetValue("X-Tenant-Code", out var headerTCode) && !string.IsNullOrWhiteSpace(headerTCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == headerTCode.ToString().ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }

        var query = _context.Sales
            .IgnoreQueryFilters()
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        if (targetTenantId > 0)
        {
            query = query.Where(s => s.TenantId == targetTenantId);
        }

        var sales = await query
            .OrderByDescending(s => s.ID)
            .Select(s => new
            {
                id = s.InvoiceNumber ?? $"INV-{s.ID}",
                dbId = s.ID,
                tenantId = s.TenantId,
                customerName = s.Customer != null ? s.Customer.Name : "Walk-in Customer",
                totalAmount = (double)s.TotalAmount,
                paidAmount = (double)s.PaidAmount,
                udhaarAmount = (double)s.UdhaarAmount,
                paymentMode = s.PaymentMode ?? "Cash",
                createdAt = s.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                status = s.PaymentMode == "Udhaar" ? "PENDING_CREDIT" : "COMPLETED",
                itemsCount = s.SaleItems.Count > 0 ? s.SaleItems.Count : 1,
                items = s.SaleItems.Select(si => new
                {
                    productId = si.ProductId,
                    productName = si.ProductName,
                    quantity = (double)si.Quantity,
                    unitPrice = (double)si.UnitPrice,
                    taxPercent = (double)si.TaxPercent,
                    totalAmount = (double)si.TotalAmount
                }).ToList()
            })
            .ToListAsync();

        return Ok(sales);
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> CreateSale([FromBody] CreateSaleRequest request)
    {
        if (request == null) request = new CreateSaleRequest();
        if ((!request.TenantId.HasValue || request.TenantId <= 0) && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            request.TenantId = headerTid;
        }
        if (string.IsNullOrWhiteSpace(request.TenantCode) && Request.Headers.TryGetValue("X-Tenant-Code", out var headerTCode) && !string.IsNullOrWhiteSpace(headerTCode))
        {
            request.TenantCode = headerTCode.ToString();
        }

        var response = await _saleService.CreateSaleAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSaleById(string id)
    {
        var sale = await _context.Sales
            .IgnoreQueryFilters()
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.InvoiceNumber == id || s.ID.ToString() == id);

        if (sale == null) return NotFound(PostResponse.Error("Sale transaction not found.", 404));

        return Ok(new
        {
            id = sale.InvoiceNumber ?? $"INV-{sale.ID}",
            dbId = sale.ID,
            customerName = sale.Customer != null ? sale.Customer.Name : "Walk-in Customer",
            totalAmount = (double)sale.TotalAmount,
            paidAmount = (double)sale.PaidAmount,
            udhaarAmount = (double)sale.UdhaarAmount,
            paymentMode = sale.PaymentMode ?? "Cash",
            createdAt = sale.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
            items = sale.SaleItems.Select(si => new
            {
                productId = si.ProductId,
                productName = si.ProductName,
                quantity = (double)si.Quantity,
                unitPrice = (double)si.UnitPrice,
                taxPercent = (double)si.TaxPercent,
                taxAmount = (double)si.TaxAmount,
                totalAmount = (double)si.TotalAmount
            }).ToList()
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSale(string id)
    {
        var sale = await _context.Sales
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.InvoiceNumber == id || s.ID.ToString() == id);

        if (sale != null)
        {
            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();
            return Ok(PostResponse.Success("Sale deleted successfully."));
        }
        return NotFound(PostResponse.Error("Sale not found.", 404));
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllSales([FromQuery] long? tenantId)
    {
        var salesQuery = _context.Sales.IgnoreQueryFilters().AsQueryable();
        if (tenantId.HasValue && tenantId.Value > 0)
        {
            salesQuery = salesQuery.Where(s => s.TenantId == tenantId.Value);
        }
        var sales = await salesQuery.ToListAsync();
        _context.Sales.RemoveRange(sales);
        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success($"Cleared {sales.Count} sales records."));
    }
}
