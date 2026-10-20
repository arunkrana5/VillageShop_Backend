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
    public async Task<IActionResult> GetSales(
        [FromQuery] long? tenantId,
        [FromQuery] string? tenantCode,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 0,
        [FromQuery] string? sortBy = "id",
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool paged = false)
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

        bool isAsc = string.Equals(sortDirection, "asc", System.StringComparison.OrdinalIgnoreCase);

        try
        {
            var query = _context.Sales
                .IgnoreQueryFilters()
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .Include(s => s.SalePayments)
                .AsNoTracking()
                .Where(s => !s.IsDeleted);

            if (targetTenantId > 0)
            {
                query = query.Where(s => s.TenantId == targetTenantId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower().Trim();
                query = query.Where(s => (s.InvoiceNumber != null && s.InvoiceNumber.ToLower().Contains(term)) ||
                                         (s.Customer != null && s.Customer.Name.ToLower().Contains(term)) ||
                                         (s.Customer != null && s.Customer.Mobile != null && s.Customer.Mobile.Contains(term)));
            }

            if (startDate.HasValue)
            {
                query = query.Where(s => s.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.CreatedDate <= endDate.Value);
            }

            int totalCount = await query.CountAsync();

            var ordered = (sortBy?.Trim().ToLower()) switch
            {
                "date" or "createddate" => isAsc ? query.OrderBy(s => s.CreatedDate).ThenByDescending(s => s.ID) : query.OrderByDescending(s => s.CreatedDate).ThenByDescending(s => s.ID),
                "totalamount" or "amount" => isAsc ? query.OrderBy(s => s.TotalAmount).ThenByDescending(s => s.ID) : query.OrderByDescending(s => s.TotalAmount).ThenByDescending(s => s.ID),
                "customer" => isAsc ? query.OrderBy(s => s.Customer != null ? s.Customer.Name : "").ThenByDescending(s => s.ID) : query.OrderByDescending(s => s.Customer != null ? s.Customer.Name : "").ThenByDescending(s => s.ID),
                _ => isAsc ? query.OrderBy(s => s.ID) : query.OrderByDescending(s => s.ID)
            };

            if (pageSize > 0)
            {
                Response.Headers["X-Total-Count"] = totalCount.ToString();
                Response.Headers["X-Page-Number"] = page.ToString();
                Response.Headers["X-Page-Size"] = pageSize.ToString();
                bool hasMore = page * pageSize < totalCount;
                Response.Headers["X-Has-More"] = hasMore.ToString().ToLower();
                ordered = (IOrderedQueryable<Sale>)ordered.Skip((page - 1) * pageSize).Take(pageSize);
            }

            var sales = await ordered
                .Select(s => new
                {
                    id = s.InvoiceNumber ?? $"INV-{s.ID}",
                    dbId = s.ID,
                    tenantId = s.TenantId,
                    customerId = s.CustomerId,
                    customerName = s.Customer != null ? s.Customer.Name : "Walk-in Customer",
                    customerMobile = s.Customer != null ? s.Customer.Mobile : "",
                    customerPhone = s.Customer != null ? s.Customer.Mobile : "",
                    phone = s.Customer != null ? s.Customer.Mobile : "",
                    mobile = s.Customer != null ? s.Customer.Mobile : "",
                    subTotal = (double)s.SubTotal,
                    grossAmount = (double)s.SubTotal,
                    taxAmount = (double)s.TaxAmount,
                    discountAmount = (double)s.DiscountAmount,
                    discount = (double)s.DiscountAmount,
                    totalAmount = (double)s.TotalAmount,
                    netAmount = (double)s.TotalAmount,
                    paidAmount = (double)s.PaidAmount,
                    udhaarAmount = (double)s.UdhaarAmount,
                    paymentMode = s.PaymentMode ?? "Cash",
                    createdAt = s.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    status = s.UdhaarAmount > 0 ? (s.PaidAmount > 0 ? "PARTIALLY_PAID" : "PENDING_CREDIT") : "COMPLETED",
                    itemsCount = s.SaleItems.Count > 0 ? s.SaleItems.Count : 1,
                    items = s.SaleItems.Select(si => new
                    {
                        productId = si.ProductId,
                        productName = si.ProductName,
                        quantity = (double)si.Quantity,
                        unitPrice = (double)si.UnitPrice,
                        rate = (double)si.UnitPrice,
                        price = (double)si.UnitPrice,
                        taxPercent = (double)si.TaxPercent,
                        totalAmount = (double)si.TotalAmount,
                        total = (double)si.TotalAmount
                    }).ToList(),
                    payments = s.SalePayments.Select(sp => new
                    {
                        paymentMode = sp.PaymentMode,
                        amount = (double)sp.Amount,
                        upiIdUsed = sp.UpiIdUsed,
                        accountName = sp.AccountName,
                        bankName = sp.BankName,
                        transactionRef = sp.TransactionRef,
                        isReceived = sp.IsReceived,
                        paymentDate = sp.PaymentDate.ToString("yyyy-MM-dd hh:mm tt")
                    }).ToList()
                })
                .ToListAsync();

            bool returnPaged = paged || (Request.Query.ContainsKey("paged") && Request.Query["paged"] == "true") || (Request.Query.ContainsKey("page") && pageSize > 0 && Request.Query.ContainsKey("pageSize"));
            if (returnPaged && pageSize > 0)
            {
                return Ok(PagedResult<object>.Create(sales.Cast<object>().ToList(), totalCount, page, pageSize));
            }

            return Ok(sales);
        }
        catch (System.Exception ex)
        {
            // Fallback query without SalePayments if V_SalePayments table is missing or migrating
            try
            {
                var fallbackQuery = _context.Sales
                    .IgnoreQueryFilters()
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted);

                if (targetTenantId > 0)
                {
                    fallbackQuery = fallbackQuery.Where(s => s.TenantId == targetTenantId);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.ToLower().Trim();
                    fallbackQuery = fallbackQuery.Where(s => (s.InvoiceNumber != null && s.InvoiceNumber.ToLower().Contains(term)) ||
                                                             (s.Customer != null && s.Customer.Name.ToLower().Contains(term)) ||
                                                             (s.Customer != null && s.Customer.Mobile != null && s.Customer.Mobile.Contains(term)));
                }

                int totalCount = await fallbackQuery.CountAsync();

                var fallbackOrdered = isAsc ? fallbackQuery.OrderBy(s => s.ID) : fallbackQuery.OrderByDescending(s => s.ID);

                if (pageSize > 0)
                {
                    Response.Headers["X-Total-Count"] = totalCount.ToString();
                    Response.Headers["X-Page-Number"] = page.ToString();
                    Response.Headers["X-Page-Size"] = pageSize.ToString();
                    bool hasMore = page * pageSize < totalCount;
                    Response.Headers["X-Has-More"] = hasMore.ToString().ToLower();
                    fallbackOrdered = (IOrderedQueryable<Sale>)fallbackOrdered.Skip((page - 1) * pageSize).Take(pageSize);
                }

                var fallbackSales = await fallbackOrdered
                    .Select(s => new
                    {
                        id = s.InvoiceNumber ?? $"INV-{s.ID}",
                        dbId = s.ID,
                        tenantId = s.TenantId,
                        customerId = s.CustomerId,
                        customerName = s.Customer != null ? s.Customer.Name : "Walk-in Customer",
                        customerMobile = s.Customer != null ? s.Customer.Mobile : "",
                        customerPhone = s.Customer != null ? s.Customer.Mobile : "",
                        phone = s.Customer != null ? s.Customer.Mobile : "",
                        mobile = s.Customer != null ? s.Customer.Mobile : "",
                        subTotal = (double)s.SubTotal,
                        grossAmount = (double)s.SubTotal,
                        taxAmount = (double)s.TaxAmount,
                        discountAmount = (double)s.DiscountAmount,
                        discount = (double)s.DiscountAmount,
                        totalAmount = (double)s.TotalAmount,
                        netAmount = (double)s.TotalAmount,
                        paidAmount = (double)s.PaidAmount,
                        udhaarAmount = (double)s.UdhaarAmount,
                        paymentMode = s.PaymentMode ?? "Cash",
                        createdAt = s.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                        status = s.UdhaarAmount > 0 ? (s.PaidAmount > 0 ? "PARTIALLY_PAID" : "PENDING_CREDIT") : "COMPLETED",
                        itemsCount = s.SaleItems.Count > 0 ? s.SaleItems.Count : 1,
                        items = s.SaleItems.Select(si => new
                        {
                            productId = si.ProductId,
                            productName = si.ProductName,
                            quantity = (double)si.Quantity,
                            unitPrice = (double)si.UnitPrice,
                            rate = (double)si.UnitPrice,
                            price = (double)si.UnitPrice,
                            taxPercent = (double)si.TaxPercent,
                            totalAmount = (double)si.TotalAmount,
                            total = (double)si.TotalAmount
                        }).ToList(),
                        payments = new System.Collections.Generic.List<object>()
                    })
                    .ToListAsync();

                bool returnPaged = paged || (Request.Query.ContainsKey("paged") && Request.Query["paged"] == "true") || (Request.Query.ContainsKey("page") && pageSize > 0 && Request.Query.ContainsKey("pageSize"));
                if (returnPaged && pageSize > 0)
                {
                    return Ok(PagedResult<object>.Create(fallbackSales.Cast<object>().ToList(), totalCount, page, pageSize));
                }

                return Ok(fallbackSales);
            }
            catch (System.Exception)

            {
                return Ok(new System.Collections.Generic.List<object>());
            }
        }
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
            .Include(s => s.SalePayments)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.InvoiceNumber == id || s.ID.ToString() == id);

        if (sale == null) return NotFound(PostResponse.Error("Sale transaction not found.", 404));

        return Ok(new
        {
            id = sale.InvoiceNumber ?? $"INV-{sale.ID}",
            dbId = sale.ID,
            customerId = sale.CustomerId,
            customerName = sale.Customer != null ? sale.Customer.Name : "Walk-in Customer",
            customerMobile = sale.Customer != null ? sale.Customer.Mobile : "",
            customerPhone = sale.Customer != null ? sale.Customer.Mobile : "",
            phone = sale.Customer != null ? sale.Customer.Mobile : "",
            mobile = sale.Customer != null ? sale.Customer.Mobile : "",
            subTotal = (double)sale.SubTotal,
            grossAmount = (double)sale.SubTotal,
            taxAmount = (double)sale.TaxAmount,
            discountAmount = (double)sale.DiscountAmount,
            discount = (double)sale.DiscountAmount,
            totalAmount = (double)sale.TotalAmount,
            netAmount = (double)sale.TotalAmount,
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
            }).ToList(),
            payments = sale.SalePayments.Select(sp => new
            {
                paymentMode = sp.PaymentMode,
                amount = (double)sp.Amount,
                upiIdUsed = sp.UpiIdUsed,
                transactionRef = sp.TransactionRef,
                isReceived = sp.IsReceived,
                paymentDate = sp.PaymentDate.ToString("yyyy-MM-dd hh:mm tt")
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

    [HttpPost("send-whatsapp")]
    public async Task<IActionResult> SendWhatsAppGateway([FromBody] SendWhatsAppRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(PostResponse.Error("Phone number and message content are required.", 400));
        }

        long targetTenantId = request.TenantId ?? 0;
        if (targetTenantId <= 0 && Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            targetTenantId = headerTid;
        }

        var result = await _saleService.SendServerWhatsAppAsync(request.Phone, request.Message, targetTenantId);

        if (result.Success)
        {
            return Ok(PostResponse.Success(result.Message, id: 0, additionalMessage: result.MessageId ?? ""));
        }
        else
        {
            return StatusCode(result.StatusCode, PostResponse.Error(result.Message, result.StatusCode, additionalMessage: result.ErrorDetails ?? ""));
        }
    }
}

public class SendWhatsAppRequest
{
    public long? TenantId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
