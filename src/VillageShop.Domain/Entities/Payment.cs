using System;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Payment : BaseTenantEntity
{
    public string PaymentNumber { get; set; } = string.Empty;

    public string? ClientTransactionId { get; set; }

    public long? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public long? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public long? SaleId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMode { get; set; } = "Cash";

    public string? TransactionReference { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
