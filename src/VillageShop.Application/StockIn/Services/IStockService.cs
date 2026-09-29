using System.Collections.Generic;
using System.Threading.Tasks;
using VillageShop.Application.StockIn.DTOs;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Application.StockIn.Services;

public interface IStockService
{
    Task<PostResponse> CreateAsync(CreateStockInRequest request);
    Task<PostResponse> UpdateAsync(UpdateStockInRequest request);
    Task<PostResponse> DeleteAsync(long id);
    Task<Stock?> GetByIdAsync(long id);
    Task<IEnumerable<Stock>> SearchAsync(StockInSearchRequest request);
}
