using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.Sales.DTOs;
using VillageShop.Application.Sales.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly IApplicationDbContext _context;

    public SaleService(IApplicationDbContext context)
    {
        _context = context;
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

        // 2. Resolve Customer by ID or Name
        Customer? targetCustomer = null;
        if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
        {
            targetCustomer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ID == request.CustomerId.Value && !c.IsDeleted);
        }
        if (targetCustomer == null && !string.IsNullOrWhiteSpace(request.CustomerName) && request.CustomerName.ToLower() != "walk-in customer")
        {
            targetCustomer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name.ToLower() == request.CustomerName.ToLower() && !c.IsDeleted);
            if (targetCustomer == null)
            {
                targetCustomer = new Customer
                {
                    TenantId = 1,
                    Name = request.CustomerName,
                    Mobile = "",
                    CurrentBalance = 0
                };
                _context.Customers.Add(targetCustomer);
                await _context.SaveChangesAsync();
            }
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

            Product? product = null;
            if (targetPid > 0)
            {
                product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ID == targetPid && !p.IsDeleted);
            }
            if (product == null && !string.IsNullOrWhiteSpace(targetPName))
            {
                product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Name.ToLower() == targetPName.ToLower() && !p.IsDeleted);
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
        
        string paymentMode = string.IsNullOrWhiteSpace(request.PaymentMode) ? "Cash" : request.PaymentMode;
        decimal paidAmount = request.PaidAmount;
        if (paymentMode.Equals("Cash", StringComparison.OrdinalIgnoreCase) || paymentMode.Equals("UPI", StringComparison.OrdinalIgnoreCase))
        {
            if (paidAmount <= 0) paidAmount = finalTotalAmount;
        }

        decimal udhaarAmount = finalTotalAmount - paidAmount;
        if (udhaarAmount < 0) udhaarAmount = 0;

        var sale = new Sale
        {
            TenantId = targetCustomer?.TenantId ?? 1,
            InvoiceNumber = invoiceNumber,
            ClientTransactionId = clientTxId,
            CustomerId = targetCustomer?.ID,
            SaleDate = DateTime.UtcNow,
            SubTotal = subTotal,
            TaxAmount = totalTax,
            DiscountAmount = request.DiscountAmount,
            TotalAmount = finalTotalAmount,
            PaidAmount = paidAmount,
            UdhaarAmount = udhaarAmount,
            PaymentMode = paymentMode,
            Notes = request.Notes ?? "",
            SaleItems = saleItems
        };

        _context.Sales.Add(sale);

        // 3. Udhaar / Credit Balance Handling
        if (targetCustomer != null && udhaarAmount > 0)
        {
            targetCustomer.CurrentBalance += udhaarAmount;

            var udhaarLedger = new UdhaarLedger
            {
                TenantId = targetCustomer.TenantId,
                CustomerId = targetCustomer.ID,
                TransactionDate = DateTime.UtcNow,
                TransactionType = "CREDIT_SALE",
                DebitAmount = udhaarAmount,
                CreditAmount = 0,
                RunningBalance = targetCustomer.CurrentBalance,
                Description = $"Udhaar Credit for Invoice {invoiceNumber}"
            };
            _context.UdhaarLedgers.Add(udhaarLedger);
        }

        await _context.SaveChangesAsync();
        return PostResponse.Success("Sale completed successfully.", sale.ID, invoiceNumber);
    }

    public async Task<Sale?> GetSaleByIdAsync(long id)
    {
        return await _context.Sales
            .IgnoreQueryFilters()
            .Include(s => s.SaleItems)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.ID == id && !s.IsDeleted);
    }
}
