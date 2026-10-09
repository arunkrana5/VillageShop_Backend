using System.Collections.Generic;

namespace VillageShop.Application.Sales.DTOs;

public class CreateSaleItemRequest
{
    private long _productId;
    public long ProductId
    {
        get => _productId;
        set => _productId = value > 0 ? value : _productId;
    }
    public long Id
    {
        get => _productId;
        set => _productId = value > 0 ? value : _productId;
    }

    private string? _productName;
    public string? ProductName
    {
        get => _productName;
        set => _productName = !string.IsNullOrWhiteSpace(value) ? value : _productName;
    }
    public string? Name
    {
        get => _productName;
        set => _productName = !string.IsNullOrWhiteSpace(value) ? value : _productName;
    }

    private decimal _quantity;
    public decimal Quantity
    {
        get => _quantity;
        set => _quantity = value > 0 ? value : _quantity;
    }
    public decimal Qty
    {
        get => _quantity;
        set => _quantity = value > 0 ? value : _quantity;
    }

    private decimal _unitPrice;
    public decimal UnitPrice
    {
        get => _unitPrice;
        set => _unitPrice = value > 0 ? value : _unitPrice;
    }
    public decimal Price
    {
        get => _unitPrice;
        set => _unitPrice = value > 0 ? value : _unitPrice;
    }

    public decimal TaxPercent { get; set; }
}

public class CreateSaleRequest
{
    public long? TenantId { get; set; }

    public string? TenantCode { get; set; }

    public string ClientTransactionId { get; set; } = string.Empty;

    public long? CustomerId { get; set; }

    private string? _customerName;
    public string? CustomerName
    {
        get => _customerName;
        set => _customerName = value;
    }
    public string? Customer
    {
        get => _customerName;
        set => _customerName = string.IsNullOrWhiteSpace(_customerName) ? value : _customerName;
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set => _totalAmount = value;
    }
    public decimal Amount
    {
        get => _totalAmount;
        set => _totalAmount = value > 0 ? value : _totalAmount;
    }

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public string PaymentMode { get; set; } = "Cash";

    public string Notes { get; set; } = string.Empty;

    public List<CreateSaleItemRequest> Items { get; set; } = new();

    public List<SalePaymentDto>? Payments { get; set; }
}

public class SalePaymentDto
{
    public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Udhaar, Card
    public decimal Amount { get; set; }
    public string? UpiIdUsed { get; set; }
    public string? AccountName { get; set; }
    public string? BankName { get; set; }
    public string? TransactionRef { get; set; }
    public bool IsReceived { get; set; } = true;
    public string? Notes { get; set; }
}

public class SaleDto
{
    public long ID { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ClientTransactionId { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerMobile { get; set; }
    public System.DateTime SaleDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UdhaarAmount { get; set; }
    public string PaymentMode { get; set; } = "Cash";
    public string PaymentStatus { get; set; } = "Paid"; // Paid, Partially Paid, Udhaar
    public string? Notes { get; set; }
    public List<SalePaymentDto> Payments { get; set; } = new();
}
