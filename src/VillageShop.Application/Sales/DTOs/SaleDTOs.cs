using System.Collections.Generic;

namespace VillageShop.Application.Sales.DTOs;

public class CreateSaleItemRequest
{
    public long ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TaxPercent { get; set; }
}

public class CreateSaleRequest
{
    public string ClientTransactionId { get; set; } = string.Empty; // Unique offline GUID

    public long? CustomerId { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public string PaymentMode { get; set; } = "Cash";

    public string Notes { get; set; } = string.Empty;

    public List<CreateSaleItemRequest> Items { get; set; } = new();
}
