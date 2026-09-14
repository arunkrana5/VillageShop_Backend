using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.Sales.DTOs;
using VillageShop.Domain.Entities;
using VillageShop.Infrastructure.EFCore;
using VillageShop.Infrastructure.Services;
using Xunit;

namespace VillageShop.Tests;

public class SaleServiceTests
{
    private class TestCurrentTenantService : ICurrentTenantService
    {
        public long TenantId => 1;
        public long UserId => 10;
        public string Username => "shopkeeper";
        public string RoleName => "Admin";
        public bool IsSuperAdmin => false;
        public void SetTenantId(long tenantId) { }
    }

    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var tenantService = new TestCurrentTenantService();
        return new ApplicationDbContext(options, tenantService);
    }

    [Fact]
    public async Task CreateSaleAsync_EnforcesIdempotency_WhenDuplicateClientTransactionIdReceived()
    {
        var context = GetInMemoryDbContext();
        var saleService = new SaleService(context);

        // Seed product
        var product = new Product
        {
            TenantId = 1,
            Name = "Basmati Rice 5kg",
            PurchasePrice = 300,
            SellingPrice = 400,
            CurrentStock = 50,
            OpeningStock = 50
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var clientTxId = "TXN-OFFLINE-GUID-99999";
        var request = new CreateSaleRequest
        {
            ClientTransactionId = clientTxId,
            PaidAmount = 400,
            Items = new List<CreateSaleItemRequest>
            {
                new CreateSaleItemRequest { ProductId = product.ID, Quantity = 1, UnitPrice = 400, TaxPercent = 0 }
            }
        };

        // First attempt
        var response1 = await saleService.CreateSaleAsync(request);
        Assert.True(response1.Status);

        // Verify stock deducted to 49
        var updatedProduct = await context.Products.FindAsync(product.ID);
        Assert.Equal(49, updatedProduct!.CurrentStock);

        // Duplicate second attempt with SAME ClientTransactionId
        var response2 = await saleService.CreateSaleAsync(request);

        // Must succeed idempotently without creating a second sale or deducting stock twice!
        Assert.True(response2.Status);
        Assert.Contains("Idempotent", response2.Message);

        var saleCount = await context.Sales.CountAsync();
        Assert.Equal(1, saleCount); // ONLY 1 sale record exists

        // Stock must still be 49 (not 48!)
        Assert.Equal(49, updatedProduct.CurrentStock);
    }
}
