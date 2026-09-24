using System.Collections.Generic;

namespace VillageShop.Application.Sales.DTOs;

public class CreateSaleItemRequest
{
    private long _productId;
    public long ProductId
    {
        get => _productId;
        set => _productId = value;
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
        set => _productName = value;
    }
    public string? Name
    {
        get => _productName;
        set => _productName = string.IsNullOrWhiteSpace(_productName) ? value : _productName;
    }

    private decimal _quantity;
    public decimal Quantity
    {
        get => _quantity;
        set => _quantity = value;
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
        set => _unitPrice = value;
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
}
