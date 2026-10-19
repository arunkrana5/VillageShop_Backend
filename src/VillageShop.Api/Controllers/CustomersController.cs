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
    public async Task<IActionResult> GetCustomers(
        [FromQuery] long? tenantId,
        [FromQuery] string? tenantCode,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 0,
        [FromQuery] string? sortBy = "id",
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] bool udhaarOnly = false,
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

        var query = _context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (targetTenantId > 0)
        {
            query = query.Where(c => c.TenantId == targetTenantId);
        }

        if (udhaarOnly)
        {
            query = query.Where(c => c.CurrentBalance > 0);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower().Trim();
            query = query.Where(c => (c.Name != null && c.Name.ToLower().Contains(s)) ||
                                     (c.Mobile != null && c.Mobile.Contains(s)) ||
                                     (c.Village != null && c.Village.ToLower().Contains(s)) ||
                                     (c.FatherName != null && c.FatherName.ToLower().Contains(s)));
        }

        int totalCount = await query.CountAsync();

        bool isAsc = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var ordered = (sortBy?.Trim().ToLower()) switch
        {
            "name" => isAsc ? query.OrderBy(c => c.Name).ThenByDescending(c => c.ID) : query.OrderByDescending(c => c.Name).ThenByDescending(c => c.ID),
            "udhaar" or "balance" or "currentbalance" => isAsc ? query.OrderBy(c => c.CurrentBalance).ThenByDescending(c => c.ID) : query.OrderByDescending(c => c.CurrentBalance).ThenByDescending(c => c.ID),
            "date" or "modifieddate" => isAsc ? query.OrderBy(c => c.ModifiedDate).ThenByDescending(c => c.ID) : query.OrderByDescending(c => c.ModifiedDate).ThenByDescending(c => c.ID),
            _ => isAsc ? query.OrderBy(c => c.ID) : query.OrderByDescending(c => c.ID)
        };

        if (pageSize > 0)
        {
            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page-Number"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
            bool hasMore = page * pageSize < totalCount;
            Response.Headers["X-Has-More"] = hasMore.ToString().ToLower();
            ordered = (IOrderedQueryable<Customer>)ordered.Skip((page - 1) * pageSize).Take(pageSize);
        }

        var customers = await ordered
            .Select(c => new
            {
                id = c.ID.ToString(),
                tenantId = c.TenantId,
                name = c.Name,
                phone = c.Mobile ?? "",
                mobile = c.Mobile ?? "",
                customerMobile = c.Mobile ?? "",
                customerPhone = c.Mobile ?? "",
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

        bool returnPaged = paged || (Request.Query.ContainsKey("paged") && Request.Query["paged"] == "true") || (Request.Query.ContainsKey("page") && pageSize > 0 && Request.Query.ContainsKey("pageSize"));
        if (returnPaged && pageSize > 0)
        {
            return Ok(PagedResult<object>.Create(customers.Cast<object>().ToList(), totalCount, page, pageSize));
        }

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

    [HttpGet("{identifier}/ledger")]
    public async Task<IActionResult> GetCustomerLedger(string identifier)
    {
        long targetTenantId = 0;
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var headerTidStr) && long.TryParse(headerTidStr, out var headerTid) && headerTid > 0)
        {
            targetTenantId = headerTid;
        }
        if (targetTenantId <= 0 && Request.Headers.TryGetValue("X-Tenant-Code", out var headerTCode) && !string.IsNullOrWhiteSpace(headerTCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == headerTCode.ToString().ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }
        if (targetTenantId <= 0 && Request.Query.TryGetValue("tenantId", out var qTidStr) && long.TryParse(qTidStr, out var qTid) && qTid > 0)
        {
            targetTenantId = qTid;
        }

        string decodedId = Uri.UnescapeDataString(identifier).Trim();
        Customer? customer = null;
        if (long.TryParse(decodedId, out long id) && id > 0)
        {
            if (targetTenantId > 0)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == id && c.TenantId == targetTenantId && !c.IsDeleted);
            }
            if (customer == null)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);
            }
        }
        if (customer == null && !string.IsNullOrWhiteSpace(decodedId))
        {
            if (targetTenantId > 0)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == targetTenantId && c.Name.ToLower() == decodedId.ToLower() && !c.IsDeleted);
            }
            if (customer == null)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name.ToLower() == decodedId.ToLower() && !c.IsDeleted);
            }
        }
        if (customer == null && !string.IsNullOrWhiteSpace(decodedId))
        {
            if (targetTenantId > 0)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == targetTenantId && c.Mobile == decodedId && !c.IsDeleted);
            }
            if (customer == null)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Mobile == decodedId && !c.IsDeleted);
            }
        }

        if (customer == null)
        {
            return NotFound(PostResponse.Error("Customer not found.", 404));
        }

        var ledgers = await _context.UdhaarLedgers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.CustomerId == customer.ID && !l.IsDeleted)
            .OrderBy(l => l.TransactionDate)
            .ToListAsync();

        var customerSales = await _context.Sales
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(s => s.SalePayments)
            .Include(s => s.SaleItems)
            .Where(s => s.CustomerId == customer.ID && !s.IsDeleted)
            .OrderBy(s => s.SaleDate)
            .ToListAsync();

        var salesDict = customerSales.ToDictionary(s => s.ID);
        var linkedSaleIds = ledgers.Where(l => l.SaleId.HasValue).Select(l => l.SaleId!.Value).ToHashSet();

        var resultList = new List<object>();

        // 1. Process all existing ledger records
        foreach (var l in ledgers)
        {
            Sale? linkedSale = l.SaleId.HasValue && salesDict.ContainsKey(l.SaleId.Value) ? salesDict[l.SaleId.Value] : null;

            double totalAmount = (double)(linkedSale?.TotalAmount ?? (l.DebitAmount > 0 ? l.DebitAmount : 0));
            double paidAmount = (double)(linkedSale?.PaidAmount ?? l.CreditAmount);
            double cashAmount = (double)(linkedSale?.SalePayments?.Where(p => p.PaymentMode.Equals("Cash", StringComparison.OrdinalIgnoreCase) && p.IsReceived).Sum(p => p.Amount) ?? 0);
            double upiAmount = (double)(linkedSale?.SalePayments?.Where(p => p.PaymentMode.Equals("UPI", StringComparison.OrdinalIgnoreCase) && p.IsReceived).Sum(p => p.Amount) ?? 0);
            double udhaarAmount = (double)(linkedSale?.UdhaarAmount ?? (l.DebitAmount > l.CreditAmount ? l.DebitAmount - l.CreditAmount : 0));
            string invoiceNo = linkedSale?.InvoiceNumber ?? "";
            string paymentMode = linkedSale?.PaymentMode ?? (l.DebitAmount > 0 ? (udhaarAmount > 0 ? "Udhaar" : "Cash") : "Payment Received");

            string itemsSummary = "";
            if (linkedSale?.SaleItems != null && linkedSale.SaleItems.Any())
            {
                itemsSummary = string.Join(", ", linkedSale.SaleItems.Take(4).Select(i => $"{i.Quantity}x {i.ProductName}"));
                if (linkedSale.SaleItems.Count > 4) itemsSummary += $", +{linkedSale.SaleItems.Count - 4} more";
            }

            // Standardize Debit and Credit:
            // For a sale: Debit = Total Bill Amount, Credit = Paid on the spot
            // For a standalone payment: Debit = 0, Credit = Paid Amount
            double debitVal = l.SaleId.HasValue ? (totalAmount > 0 ? totalAmount : (double)l.DebitAmount) : (double)l.DebitAmount;
            double creditVal = l.SaleId.HasValue ? (paidAmount > 0 ? paidAmount : (double)l.CreditAmount) : (double)l.CreditAmount;

            string status = udhaarAmount > 0 
                ? (paidAmount > 0 ? "PARTIAL" : "UDHAAR") 
                : (l.SaleId.HasValue ? "PAID" : "RECEIVED");

            resultList.Add(new
            {
                id = l.ID.ToString(),
                date = l.TransactionDate.ToString("dd MMM yyyy, hh:mm tt"),
                rawDate = l.TransactionDate,
                type = l.TransactionType,
                invoiceNumber = invoiceNo,
                paymentMode = paymentMode,
                description = l.Description ?? (debitVal > 0 ? $"Invoice #{invoiceNo} [{paymentMode}]" : "Payment Credit"),
                itemsSummary = itemsSummary,
                totalAmount = totalAmount,
                paidAmount = paidAmount,
                cashAmount = cashAmount,
                upiAmount = upiAmount,
                udhaarAmount = udhaarAmount,
                debit = debitVal,
                credit = creditVal,
                balance = (double)l.RunningBalance,
                status = status
            });
        }

        // 2. Also incorporate any sales for this customer that were NOT in UdhaarLedgers table
        foreach (var s in customerSales.Where(s => !linkedSaleIds.Contains(s.ID)))
        {
            double cashAmt = (double)s.SalePayments.Where(p => p.PaymentMode.Equals("Cash", StringComparison.OrdinalIgnoreCase) && p.IsReceived).Sum(p => p.Amount);
            double upiAmt = (double)s.SalePayments.Where(p => p.PaymentMode.Equals("UPI", StringComparison.OrdinalIgnoreCase) && p.IsReceived).Sum(p => p.Amount);
            string txType = s.UdhaarAmount > 0 ? (s.PaidAmount > 0 ? "PARTIAL_SALE" : "CREDIT_SALE") : "CASH_SALE";
            string pDesc = $"Invoice #{s.InvoiceNumber} [{s.PaymentMode}] - Total: ₹{s.TotalAmount:F2} (Paid: ₹{s.PaidAmount:F2}, Udhaar: ₹{s.UdhaarAmount:F2})";

            string itemsSummary = "";
            if (s.SaleItems != null && s.SaleItems.Any())
            {
                itemsSummary = string.Join(", ", s.SaleItems.Take(4).Select(i => $"{i.Quantity}x {i.ProductName}"));
                if (s.SaleItems.Count > 4) itemsSummary += $", +{s.SaleItems.Count - 4} more";
            }

            string status = s.UdhaarAmount > 0 ? (s.PaidAmount > 0 ? "PARTIAL" : "UDHAAR") : "PAID";

            resultList.Add(new
            {
                id = $"sale-{s.ID}",
                date = s.SaleDate.ToString("dd MMM yyyy, hh:mm tt"),
                rawDate = s.SaleDate,
                type = txType,
                invoiceNumber = s.InvoiceNumber,
                paymentMode = s.PaymentMode,
                description = pDesc,
                itemsSummary = itemsSummary,
                totalAmount = (double)s.TotalAmount,
                paidAmount = (double)s.PaidAmount,
                cashAmount = cashAmt,
                upiAmount = upiAmt,
                udhaarAmount = (double)s.UdhaarAmount,
                debit = (double)s.TotalAmount,
                credit = (double)s.PaidAmount,
                balance = (double)customer.CurrentBalance,
                status = status
            });
        }

        // Sort all transactions chronologically
        resultList = resultList.OrderBy(x => ((dynamic)x).rawDate).ToList();

        // Calculate summary metrics
        double totalPurchases = (double)customerSales.Sum(s => s.TotalAmount);
        double totalPaidSales = (double)customerSales.Sum(s => s.PaidAmount);
        double totalStandalonePayments = (double)ledgers.Where(l => !l.SaleId.HasValue).Sum(l => l.CreditAmount);
        double totalPaidOverall = totalPaidSales + totalStandalonePayments;

        return Ok(new
        {
            customer = new
            {
                id = customer.ID.ToString(),
                name = customer.Name,
                phone = customer.Mobile ?? "",
                mobile = customer.Mobile ?? "",
                customerMobile = customer.Mobile ?? "",
                customerPhone = customer.Mobile ?? "",
                currentBalance = (double)customer.CurrentBalance,
                totalPurchases = totalPurchases,
                totalPaid = totalPaidOverall,
                salesCount = customerSales.Count
            },
            transactions = resultList
        });
    }

    [HttpPost("payment")]
    public async Task<IActionResult> RecordPayment([FromBody] CustomerPaymentRequest request)
    {
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

        Customer? customer = null;
        if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
        {
            if (targetTenantId > 0)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == request.CustomerId.Value && c.TenantId == targetTenantId && !c.IsDeleted);
            }
            if (customer == null)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == request.CustomerId.Value && !c.IsDeleted);
            }
        }
        if (customer == null && !string.IsNullOrWhiteSpace(request.CustomerName))
        {
            if (targetTenantId > 0)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == targetTenantId && c.Name.ToLower() == request.CustomerName.ToLower() && !c.IsDeleted);
            }
            if (customer == null)
            {
                customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name.ToLower() == request.CustomerName.ToLower() && !c.IsDeleted);
            }
        }

        if (customer != null)
        {
            customer.CurrentBalance = (decimal)request.RemainingUdhaar;

            var ledgerEntry = new UdhaarLedger
            {
                TenantId = customer.TenantId,
                CustomerId = customer.ID,
                TransactionDate = DateTime.UtcNow,
                TransactionType = "PAYMENT",
                DebitAmount = 0,
                CreditAmount = (decimal)request.AmountPaid,
                RunningBalance = customer.CurrentBalance,
                Description = string.IsNullOrWhiteSpace(request.Note) ? $"Payment Received (₹ {request.AmountPaid:F2})" : request.Note
            };
            _context.UdhaarLedgers.Add(ledgerEntry);

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
    public long? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public long? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public double AmountPaid { get; set; }
    public double RemainingUdhaar { get; set; }
    public string? Note { get; set; }
}
