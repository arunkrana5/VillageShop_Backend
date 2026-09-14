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
            var existingSale = await _context.Sales.FirstOrDefaultAsync(s => s.ClientTransactionId == request.ClientTransactionId);
            if (existingSale != null)
            {
                return PostResponse.Success("Transaction already processed (Idempotent success).", existingSale.ID);
            }
        }

        var clientTxId = string.IsNullOrWhiteSpace(request.ClientTransactionId) ? Guid.NewGuid().ToString() : request.ClientTransactionId;
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        decimal subTotal = 0;
        decimal totalTax = 0;
        var saleItems = new List<SaleItem>();

        foreach (var item in request.Items)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ID == item.ProductId && !p.IsDeleted);
            if (product == null)
            {
                return PostResponse.Error($"Product with ID {item.ProductId} not found.", 404);
            }

            var itemTax = (item.UnitPrice * item.Quantity) * (item.TaxPercent / 100m);
            var itemTotal = (item.UnitPrice * item.Quantity) + itemTax;

            subTotal += item.UnitPrice * item.Quantity;
            totalTax += itemTax;

            // Deduct stock safely
            product.CurrentStock -= item.Quantity;

            saleItems.Add(new SaleItem
            {
                ProductId = product.ID,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxPercent = item.TaxPercent,
                TaxAmount = itemTax,
                TotalAmount = itemTotal
            });
        }

        var totalAmount = (subTotal + totalTax) - request.DiscountAmount;
        var udhaarAmount = totalAmount - request.PaidAmount;

        var sale = new Sale
        {
            InvoiceNumber = invoiceNumber,
            ClientTransactionId = clientTxId,
            CustomerId = request.CustomerId,
            SaleDate = DateTime.UtcNow,
            SubTotal = subTotal,
            TaxAmount = totalTax,
            DiscountAmount = request.DiscountAmount,
            TotalAmount = totalAmount,
            PaidAmount = request.PaidAmount,
            UdhaarAmount = udhaarAmount > 0 ? udhaarAmount : 0,
            PaymentMode = request.PaymentMode,
            Notes = request.Notes,
            SaleItems = saleItems
        };

        _context.Sales.Add(sale);

        // Udhaar / Credit Balance Handling
        if (request.CustomerId.HasValue && udhaarAmount > 0)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ID == request.CustomerId.Value && !c.IsDeleted);
            if (customer != null)
            {
                customer.CurrentBalance += udhaarAmount;

                var udhaarLedger = new UdhaarLedger
                {
                    CustomerId = customer.ID,
                    TransactionDate = DateTime.UtcNow,
                    TransactionType = "CREDIT_SALE",
                    DebitAmount = udhaarAmount,
                    CreditAmount = 0,
                    RunningBalance = customer.CurrentBalance,
                    Description = $"Udhaar for Invoice {invoiceNumber}"
                };
                _context.UdhaarLedgers.Add(udhaarLedger);
            }
        }

        await _context.SaveChangesAsync();
        return PostResponse.Success("Sale completed successfully.", sale.ID, invoiceNumber);
    }

    public async Task<Sale?> GetSaleByIdAsync(long id)
    {
        return await _context.Sales
            .Include(s => s.SaleItems)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.ID == id && !s.IsDeleted);
    }
}
