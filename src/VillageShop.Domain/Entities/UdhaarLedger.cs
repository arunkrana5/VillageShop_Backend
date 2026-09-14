using System;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class UdhaarLedger : BaseTenantEntity
{
    public long CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public string TransactionType { get; set; } = string.Empty;

    public long? SaleId { get; set; }

    public long? PaymentId { get; set; }

    public decimal DebitAmount { get; set; }

    public decimal CreditAmount { get; set; }

    public decimal RunningBalance { get; set; }

    public string? Description { get; set; }
}
