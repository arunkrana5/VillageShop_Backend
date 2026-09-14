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
    public async Task<IActionResult> GetCustomers()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.ID)
            .Select(c => new
            {
                id = c.ID.ToString(),
                name = c.Name,
                phone = c.Mobile ?? "",
                village = c.Village ?? "Rampur",
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
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(PostResponse.Error("Customer name is required."));
        }

        var customer = new Customer
        {
            Name = request.Name,
            Mobile = request.Phone,
            Village = string.IsNullOrWhiteSpace(request.Village) ? "Rampur" : request.Village,
            CurrentBalance = 0.00m,
            TenantId = 1
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return Ok(PostResponse.Success($"Customer '{request.Name}' saved successfully.", customer.ID));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(long id, [FromBody] CustomerCreateRequest request)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);
        if (customer == null) return NotFound(PostResponse.Error("Customer not found.", 404));

        customer.Name = request.Name;
        customer.Mobile = request.Phone;
        customer.Village = string.IsNullOrWhiteSpace(request.Village) ? "Rampur" : request.Village;

        await _context.SaveChangesAsync();
        return Ok(PostResponse.Success($"Customer '{request.Name}' updated successfully.", id));
    }

    [HttpPost("payment")]
    public async Task<IActionResult> RecordPayment([FromBody] CustomerPaymentRequest request)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Name.ToLower() == request.CustomerName.ToLower() && !c.IsDeleted);
        if (customer != null)
        {
            customer.CurrentBalance = (decimal)request.RemainingUdhaar;
            await _context.SaveChangesAsync();
        }

        return Ok(PostResponse.Success($"Payment of ₹ {request.AmountPaid:F2} recorded for {request.CustomerName}."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(long id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);
        if (customer != null)
        {
            customer.IsDeleted = true;
            customer.DeletedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return Ok(PostResponse.Success("Customer deleted successfully.", id));
    }
}

public class CustomerCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Village { get; set; } = string.Empty;
}

public class CustomerPaymentRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public double AmountPaid { get; set; }
    public double RemainingUdhaar { get; set; }
}
