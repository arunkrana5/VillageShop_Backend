using System.Collections.Generic;
using System.Threading.Tasks;
using VillageShop.Application.Products.DTOs;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Application.Products.Services;

public interface IProductService
{
    Task<PostResponse> CreateAsync(CreateProductRequest request);
    Task<PostResponse> UpdateAsync(UpdateProductRequest request);
    Task<PostResponse> DeleteAsync(long id);
    Task<Product?> GetByIdAsync(long id);
    Task<IEnumerable<Product>> SearchAsync(ProductSearchRequest request);
}
