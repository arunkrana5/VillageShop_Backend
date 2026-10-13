using System;
using VillageShop.Domain.Common;

namespace VillageShop.Domain.Entities;

public class Expense : BaseTenantEntity
{
    public string ExpenseCategory { get; set; } = string.Empty;

    public long? ExpenseCategoryId { get; set; }

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

    public string PaymentMode { get; set; } = "Cash";

    public long? PaymentModeId { get; set; }

    public string? Description { get; set; }

    public string? AttachmentUrl { get; set; }
}
