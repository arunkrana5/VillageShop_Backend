using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Application.Products.DTOs;
using VillageShop.Application.Products.Services;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PostResponse> CreateAsync(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return PostResponse.Error("Product name is required.", 400);
        }

        var product = new Product
        {
            ProductCode = string.IsNullOrWhiteSpace(request.ProductCode) ? $"PRD-{Guid.NewGuid().ToString()[..6].ToUpper()}" : request.ProductCode,
            Name = request.Name,
            Category = request.Category,
            Brand = request.Brand,
            Unit = request.Unit ?? "pcs",
            Barcode = request.Barcode,
            PurchasePrice = request.PurchasePrice,
            SellingPrice = request.SellingPrice,
            MRP = request.MRP,
            GSTPercent = request.GSTPercent,
            OpeningStock = request.OpeningStock,
            MinimumStock = request.MinimumStock,
            CurrentStock = request.CurrentStock ?? request.OpeningStock
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return PostResponse.Success("Product created successfully.", product.ID);
    }

    public async Task<PostResponse> UpdateAsync(UpdateProductRequest request)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ID == request.ID && !p.IsDeleted);
        if (product == null)
        {
            return PostResponse.Error("Product not found.", 404);
        }

        product.Name = request.Name;
        product.Category = request.Category;
        product.Brand = request.Brand;
        product.Unit = request.Unit;
        product.Barcode = request.Barcode;
        product.PurchasePrice = request.PurchasePrice;
        product.SellingPrice = request.SellingPrice;
        product.MRP = request.MRP;
        product.GSTPercent = request.GSTPercent;
        product.OpeningStock = request.OpeningStock;
        product.CurrentStock = request.CurrentStock ?? request.OpeningStock;
        product.MinimumStock = request.MinimumStock;

        await _context.SaveChangesAsync();
        return PostResponse.Success("Product updated successfully.", product.ID);
    }

    public async Task<PostResponse> DeleteAsync(long id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ID == id && !p.IsDeleted);
        if (product == null)
        {
            return PostResponse.Error("Product not found.", 404);
        }

        product.IsDeleted = true;
        product.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return PostResponse.Success("Product deleted successfully.", id);
    }

    public async Task<Product?> GetByIdAsync(long id)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.ID == id && !p.IsDeleted);
    }

    public async Task<IEnumerable<Product>> SearchAsync(ProductSearchRequest request)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 200 : request.PageSize;

        var query = _context.Products.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(p => (p.Name != null && p.Name.ToLower().Contains(searchLower)) ||
                                     (p.Barcode != null && p.Barcode.ToLower().Contains(searchLower)) ||
                                     (p.ProductCode != null && p.ProductCode.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(p => p.Category == request.Category);
        }

        return await query
            .OrderByDescending(p => p.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
