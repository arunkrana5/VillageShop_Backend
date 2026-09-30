using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public CustomersController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] long? tenantId, [FromQuery] string? tenantCode)
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

        var query = _context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (targetTenantId > 0)
        {
            query = query.Where(c => c.TenantId == targetTenantId || c.TenantId == 1);
        }

        var customers = await query
            .OrderByDescending(c => c.ID)
            .Select(c => new
            {
                id = c.ID.ToString(),
                tenantId = c.TenantId,
                name = c.Name,
                phone = c.Mobile ?? "",
                email = c.Email ?? "",
                whatsapp = c.WhatsApp ?? "",
                fatherName = c.FatherName ?? "",
                address = c.Address ?? "",
                village = c.Village ?? "",
                po = c.PO ?? "",
                ps = c.PS ?? "",
                dist = c.Dist ?? "",
                pincode = c.Pincode ?? "",
                udhaar = (double)c.CurrentBalance,
                lastTx = c.ModifiedDate.ToString("dd MMM yyyy"),
                status = c.CurrentBalance > 0 ? "PENDING_CREDIT" : "COMPLETED"
            })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(PostResponse.Error("Customer name is required."));
        }

        long targetTenantId = request.TenantId ?? 0;
        if (targetTenantId <= 0 && !string.IsNullOrWhiteSpace(request.TenantCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == request.TenantCode.ToLower() && !t.IsDeleted);
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

        if (targetTenantId <= 0) targetTenantId = 1;

        var customer = new Customer
        {
            TenantId = targetTenantId,
            Name = request.Name,
            Mobile = request.Phone,
            Email = request.Email,
            WhatsApp = request.WhatsApp,
            FatherName = request.FatherName,
            Address = request.Address,
            Village = request.Village,
            PO = request.PO,
            PS = request.PS,
            Dist = request.Dist,
            Pincode = request.Pincode,
            CurrentBalance = (decimal)(request.Udhaar ?? 0)
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success($"Customer '{request.Name}' saved successfully under Tenant #{targetTenantId}.", customer.ID));
    }

    [HttpPut("{identifier}")]
    public async Task<IActionResult> UpdateCustomer(string identifier, [FromBody] CustomerCreateRequest request)
    {
        Customer? customer = null;
        if (long.TryParse(identifier, out long id))
        {
            customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);
        }
        
        if (customer == null)
        {
            customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name.ToLower() == identifier.ToLower() && !c.IsDeleted);
        }

        if (customer == null) return NotFound(PostResponse.Error("Customer not found.", 404));

        if (!string.IsNullOrWhiteSpace(request.Name)) customer.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Phone)) customer.Mobile = request.Phone;
        if (request.Email != null) customer.Email = request.Email;
        if (request.WhatsApp != null) customer.WhatsApp = request.WhatsApp;
        if (request.FatherName != null) customer.FatherName = request.FatherName;
        if (request.Address != null) customer.Address = request.Address;
        if (request.Village != null) customer.Village = request.Village;
        if (request.PO != null) customer.PO = request.PO;
        if (request.PS != null) customer.PS = request.PS;
        if (request.Dist != null) customer.Dist = request.Dist;
        if (request.Pincode != null) customer.Pincode = request.Pincode;
        if (request.Udhaar.HasValue) customer.CurrentBalance = (decimal)request.Udhaar.Value;

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success($"Customer '{customer.Name}' updated successfully.", customer.ID));
    }

    [HttpPost("payment")]
    public async Task<IActionResult> RecordPayment([FromBody] CustomerPaymentRequest request)
    {
        var customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name.ToLower() == request.CustomerName.ToLower() && !c.IsDeleted);
        if (customer != null)
        {
            customer.CurrentBalance = (decimal)request.RemainingUdhaar;
            await _context.SaveChangesAsync();
        }

        return Ok(PostResponse.Success($"Payment of ₹ {request.AmountPaid:F2} recorded for {request.CustomerName}."));
    }

    [HttpDelete("{identifier}")]
    public async Task<IActionResult> DeleteCustomer(string identifier)
    {
        Customer? customer = null;
        if (long.TryParse(identifier, out long id))
        {
            customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);
        }

        if (customer == null)
        {
            customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name.ToLower() == identifier.ToLower() && !c.IsDeleted);
        }

        if (customer != null)
        {
            customer.IsDeleted = true;
            customer.DeletedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(PostResponse.Success("Customer deleted successfully.", customer.ID));
        }
        return NotFound(PostResponse.Error("Customer not found for deletion.", 404));
    }
}

public class CustomerCreateRequest
{
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? WhatsApp { get; set; }
    public string? FatherName { get; set; }
    public string? Address { get; set; }
    public string? Village { get; set; }
    public string? PO { get; set; }
    public string? PS { get; set; }
    public string? Dist { get; set; }
    public string? Pincode { get; set; }
    public double? Udhaar { get; set; }
}

public class CustomerPaymentRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public double AmountPaid { get; set; }
    public double RemainingUdhaar { get; set; }
}
