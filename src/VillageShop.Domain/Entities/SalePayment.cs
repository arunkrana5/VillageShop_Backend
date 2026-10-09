using System;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class SalePayment : BaseTenantEntity
{
    public long SaleId { get; set; }

    public Sale? Sale { get; set; }

    public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Udhaar, Card

    public decimal Amount { get; set; }

    public string? UpiIdUsed { get; set; } // Specific UPI ID (e.g., shopkeeper@okaxis)

    public string? AccountName { get; set; } // Account Name (Holder Name)

    public string? BankName { get; set; } // Bank Name

    public string? TransactionRef { get; set; }

    public bool IsReceived { get; set; } = true; // True when payment confirmed received

    public string? Notes { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
}
