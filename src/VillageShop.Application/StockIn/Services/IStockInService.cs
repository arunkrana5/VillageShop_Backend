using System.Collections.Generic;
using System.Threading.Tasks;
using VillageShop.Application.StockIn.DTOs;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Application.StockIn.Services;

public interface IStockInService
{
    Task<PostResponse> CreateAsync(CreateStockInRequest request);
    Task<PostResponse> UpdateAsync(UpdateStockInRequest request);
    Task<PostResponse> DeleteAsync(long id);
    Task<Product?> GetByIdAsync(long id);
    Task<IEnumerable<Product>> SearchAsync(StockInSearchRequest request);
}
