using System;
using System.Collections.Generic;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Sale : BaseTenantEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public string ClientTransactionId { get; set; } = string.Empty;

    public long? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal UdhaarAmount { get; set; }

    public string PaymentMode { get; set; } = "Cash";

    public string? Notes { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
