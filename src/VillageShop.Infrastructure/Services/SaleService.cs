using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.Sales.DTOs;
using VillageShop.Application.Sales.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public SaleService(IApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<PostResponse> CreateSaleAsync(CreateSaleRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return PostResponse.Error("Sale must contain at least one item.", 400);
        }

        // 1. Idempotency Guard - check if ClientTransactionId was already processed
        if (!string.IsNullOrWhiteSpace(request.ClientTransactionId))
        {
            var existingSale = await _context.Sales.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.ClientTransactionId == request.ClientTransactionId);
            if (existingSale != null)
            {
                return PostResponse.Success("Transaction already processed (Idempotent success).", existingSale.ID, existingSale.InvoiceNumber);
            }
        }

        var clientTxId = string.IsNullOrWhiteSpace(request.ClientTransactionId) ? Guid.NewGuid().ToString() : request.ClientTransactionId;
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        // 2. Resolve Tenant ID dynamically
        long targetTenantId = request.TenantId ?? 0;
        if (targetTenantId <= 0 && !string.IsNullOrWhiteSpace(request.TenantCode))
        {
            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TenantCode.ToLower() == request.TenantCode.ToLower() && !t.IsDeleted);
            if (tenant != null) targetTenantId = tenant.ID;
        }

        // 3. Resolve Customer by ID or Name
        Customer? targetCustomer = null;
        if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
        {
            targetCustomer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == request.CustomerId.Value && !c.IsDeleted);
        }
        if (targetCustomer == null && !string.IsNullOrWhiteSpace(request.CustomerName) && request.CustomerName.ToLower() != "walk-in customer")
        {
            targetCustomer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name.ToLower() == request.CustomerName.ToLower() && !c.IsDeleted);
        }

        if ((targetTenantId <= 0 || targetTenantId == 1) && targetCustomer != null && targetCustomer.TenantId > 0)
        {
            targetTenantId = targetCustomer.TenantId;
        }

        if (targetTenantId <= 0 || targetTenantId == 1)
        {
            var firstItem = request.Items.FirstOrDefault();
            if (firstItem != null)
            {
                long pid = firstItem.ProductId > 0 ? firstItem.ProductId : firstItem.Id;
                if (pid > 0)
                {
                    var p = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.ID == pid && !x.IsDeleted);
                    if (p != null && p.TenantId > 0) targetTenantId = p.TenantId;
                }
            }
        }

        if (targetTenantId <= 0) targetTenantId = 1;

        if (targetCustomer == null && !string.IsNullOrWhiteSpace(request.CustomerName) && request.CustomerName.ToLower() != "walk-in customer")
        {
            targetCustomer = new Customer
            {
                TenantId = targetTenantId,
                Name = request.CustomerName,
                Mobile = "",
                CurrentBalance = 0
            };
            _context.Customers.Add(targetCustomer);
            await _context.SaveChangesAsync();
        }

        decimal subTotal = 0;
        decimal totalTax = 0;
        var saleItems = new List<SaleItem>();

        foreach (var item in request.Items)
        {
            long targetPid = item.ProductId > 0 ? item.ProductId : (item.Id > 0 ? item.Id : 0);
            string targetPName = !string.IsNullOrWhiteSpace(item.ProductName) ? item.ProductName : (!string.IsNullOrWhiteSpace(item.Name) ? item.Name : "Item");
            decimal itemQty = item.Quantity > 0 ? item.Quantity : (item.Qty > 0 ? item.Qty : 1);
            decimal itemPrice = item.UnitPrice > 0 ? item.UnitPrice : (item.Price > 0 ? item.Price : 0);

            Stock? product = null;
            if (targetPid > 0)
            {
                product = await _context.Stock.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ID == targetPid && !p.IsDeleted);
            }
            if (product == null && !string.IsNullOrWhiteSpace(targetPName))
            {
                product = await _context.Stock.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Name.ToLower() == targetPName.ToLower() && !p.IsDeleted);
            }

            if (product != null)
            {
                if (itemPrice <= 0) itemPrice = product.SellingPrice > 0 ? product.SellingPrice : product.MRP;
                targetPName = product.Name;
                targetPid = product.ID;

                // Deduct stock safely
                product.CurrentStock -= itemQty;
            }

            var itemTax = (itemPrice * itemQty) * (item.TaxPercent / 100m);
            var itemTotal = (itemPrice * itemQty) + itemTax;

            subTotal += itemPrice * itemQty;
            totalTax += itemTax;

            saleItems.Add(new SaleItem
            {
                TenantId = targetTenantId,
                ProductId = targetPid > 0 ? targetPid : 1,
                ProductName = targetPName,
                Quantity = itemQty,
                UnitPrice = itemPrice,
                TaxPercent = item.TaxPercent,
                TaxAmount = itemTax,
                TotalAmount = itemTotal
            });
        }

        decimal calculatedTotal = (subTotal + totalTax) - request.DiscountAmount;
        decimal finalTotalAmount = request.TotalAmount > 0 ? request.TotalAmount : (request.Amount > 0 ? request.Amount : calculatedTotal);
        
        decimal discountAmount = request.DiscountAmount;
        if (discountAmount <= 0 && (subTotal + totalTax) > finalTotalAmount)
        {
            discountAmount = (subTotal + totalTax) - finalTotalAmount;
        }
        
        decimal paidAmount = 0;
        decimal udhaarAmount = 0;
        var salePayments = new List<SalePayment>();

        if (request.Payments != null && request.Payments.Any())
        {
            foreach (var p in request.Payments)
            {
                var pMode = string.IsNullOrWhiteSpace(p.PaymentMode) ? "Cash" : p.PaymentMode;
                var pAmt = Math.Max(0, p.Amount);
                if (pAmt > 0 || pMode.Equals("Udhaar", StringComparison.OrdinalIgnoreCase))
                {
                    if (!pMode.Equals("Udhaar", StringComparison.OrdinalIgnoreCase) && p.IsReceived)
                    {
                        paidAmount += pAmt;
                    }
                    salePayments.Add(new SalePayment
                    {
                        TenantId = targetTenantId,
                        PaymentMode = pMode,
                        Amount = pAmt,
                        UpiIdUsed = pMode.Equals("UPI", StringComparison.OrdinalIgnoreCase) ? p.UpiIdUsed : null,
                        AccountName = p.AccountName,
                        BankName = p.BankName,
                        TransactionRef = p.TransactionRef,
                        IsReceived = p.IsReceived,
                        Notes = p.Notes,
                        PaymentDate = DateTime.UtcNow
                    });
                }
            }

            if (paidAmount > finalTotalAmount) paidAmount = finalTotalAmount;
            udhaarAmount = finalTotalAmount - paidAmount;
            if (udhaarAmount < 0) udhaarAmount = 0;
        }
        else
        {
            string pMode = string.IsNullOrWhiteSpace(request.PaymentMode) ? "Cash" : request.PaymentMode;
            paidAmount = request.PaidAmount;
            if (pMode.Equals("Cash", StringComparison.OrdinalIgnoreCase) || pMode.Equals("UPI", StringComparison.OrdinalIgnoreCase))
            {
                if (paidAmount <= 0) paidAmount = finalTotalAmount;
            }

            udhaarAmount = finalTotalAmount - paidAmount;
            if (udhaarAmount < 0) udhaarAmount = 0;

            if (paidAmount > 0)
            {
                salePayments.Add(new SalePayment
                {
                    TenantId = targetTenantId,
                    PaymentMode = pMode,
                    Amount = paidAmount,
                    IsReceived = true,
                    PaymentDate = DateTime.UtcNow
                });
            }
            if (udhaarAmount > 0)
            {
                salePayments.Add(new SalePayment
                {
                    TenantId = targetTenantId,
                    PaymentMode = "Udhaar",
                    Amount = udhaarAmount,
                    IsReceived = false,
                    PaymentDate = DateTime.UtcNow
                });
            }
        }

        string primaryPaymentMode = salePayments.Count == 1 ? salePayments[0].PaymentMode : (salePayments.Any(x => x.PaymentMode == "Udhaar") ? "Split/Udhaar" : "Split");

        var sale = new Sale
        {
            TenantId = targetTenantId,
            InvoiceNumber = invoiceNumber,
            ClientTransactionId = clientTxId,
            CustomerId = targetCustomer?.ID,
            SaleDate = DateTime.UtcNow,
            SubTotal = subTotal,
            TaxAmount = totalTax,
            DiscountAmount = discountAmount,
            TotalAmount = finalTotalAmount,
            PaidAmount = paidAmount,
            UdhaarAmount = udhaarAmount,
            PaymentMode = primaryPaymentMode,
            Notes = request.Notes ?? "",
            SaleItems = saleItems,
            SalePayments = salePayments
        };

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        // 3. Customer Purchase & Udhaar Ledger Tracking
        if (targetCustomer != null)
        {
            if (udhaarAmount > 0)
            {
                targetCustomer.CurrentBalance += udhaarAmount;
            }

            string txType = udhaarAmount > 0 ? (paidAmount > 0 ? "PARTIAL_SALE" : "CREDIT_SALE") : "SALE";
            string pDesc;
            if (salePayments.Any())
            {
                var modes = string.Join(", ", salePayments.Select(p => $"{p.PaymentMode}: ₹{p.Amount:F2}"));
                pDesc = $"Invoice #{invoiceNumber} - Total: ₹{finalTotalAmount:F2} ({modes})";
            }
            else
            {
                pDesc = $"Invoice #{invoiceNumber} - Total: ₹{finalTotalAmount:F2} (Paid: ₹{paidAmount:F2}, Udhaar: ₹{udhaarAmount:F2})";
            }

            var udhaarLedger = new UdhaarLedger
            {
                TenantId = targetTenantId,
                CustomerId = targetCustomer.ID,
                SaleId = sale.ID,
                TransactionDate = DateTime.UtcNow,
                TransactionType = txType,
                DebitAmount = udhaarAmount,
                CreditAmount = 0,
                RunningBalance = targetCustomer.CurrentBalance,
                Description = pDesc
            };
            _context.UdhaarLedgers.Add(udhaarLedger);
            await _context.SaveChangesAsync();
        }

        return PostResponse.Success("Sale completed successfully.", sale.ID, invoiceNumber);
    }

    public async Task<Sale?> GetSaleByIdAsync(long id)
    {
        return await _context.Sales
            .IgnoreQueryFilters()
            .Include(s => s.SaleItems)
            .Include(s => s.SalePayments)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.ID == id && !s.IsDeleted);
    }

    public async Task<bool> SendServerWhatsAppAsync(string phone, string message, long tenantId = 0)
    {
        try
        {
            string gatewayUrl = _configuration["WhatsApp:GatewayUrl"] ?? "";
            string apiKey = _configuration["WhatsApp:ApiKey"] ?? "";
            string instanceId = _configuration["WhatsApp:InstanceId"] ?? "";

            // 1. Prioritize Database TenantConfiguration table (V_TenantConfigurations)
            var dbConfig = await _context.TenantConfigurations
                .IgnoreQueryFilters()
                .AsNoTracking()
                .OrderByDescending(c => c.ID)
                .FirstOrDefaultAsync(c => (tenantId <= 0 || c.TenantId == tenantId) && !c.IsDeleted);

            if (dbConfig != null && !string.IsNullOrWhiteSpace(dbConfig.BrandingJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(dbConfig.BrandingJson);
                    var root = doc.RootElement;
                    
                    var gUrl = GetJsonStringProp(root, "whatsappGatewayUrl", "WhatsappGatewayUrl", "GatewayUrl");
                    var instId = GetJsonStringProp(root, "whatsappInstanceId", "WhatsappInstanceId", "InstanceId");
                    var key = GetJsonStringProp(root, "whatsappApiKey", "WhatsappApiKey", "ApiKey");

                    if (!string.IsNullOrWhiteSpace(gUrl)) gatewayUrl = gUrl;
                    if (!string.IsNullOrWhiteSpace(instId)) instanceId = instId;
                    if (!string.IsNullOrWhiteSpace(key)) apiKey = key;
                }
                catch (Exception) {}
            }

            if (!string.IsNullOrWhiteSpace(gatewayUrl))
            {
                using var httpClient = new HttpClient();

                // Green-API Gateway handling
                if (gatewayUrl.Contains("green-api.com", StringComparison.OrdinalIgnoreCase))
                {
                    string finalUrl = gatewayUrl;
                    if (finalUrl.Contains("{idInstance}") && !string.IsNullOrWhiteSpace(instanceId))
                        finalUrl = finalUrl.Replace("{idInstance}", instanceId);
                    if (finalUrl.Contains("{apiTokenInstance}") && !string.IsNullOrWhiteSpace(apiKey))
                        finalUrl = finalUrl.Replace("{apiTokenInstance}", apiKey);

                    string chatId = phone.EndsWith("@c.us") ? phone : $"{phone}@c.us";
                    var greenPayload = new { chatId = chatId, message = message };
                    var json = System.Text.Json.JsonSerializer.Serialize(greenPayload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var res = await httpClient.PostAsync(finalUrl, content);
                    return res.IsSuccessStatusCode;
                }
                // Generic Webhook / Cloud Gateway handling
                else
                {
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
                    }

                    var genericPayload = new
                    {
                        phone = phone,
                        chatId = $"{phone}@c.us",
                        message = message
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(genericPayload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var res = await httpClient.PostAsync(gatewayUrl, content);
                    return res.IsSuccessStatusCode;
                }
            }
        }
        catch (Exception) {}
        return true;
    }

    private static string GetJsonStringProp(System.Text.Json.JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var val) && val.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var s = val.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }
        return "";
    }
}
