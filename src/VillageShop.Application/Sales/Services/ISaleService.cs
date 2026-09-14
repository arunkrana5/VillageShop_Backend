using System.Threading.Tasks;
using VillageShop.Application.Sales.DTOs;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Application.Sales.Services;

public interface ISaleService
{
    Task<PostResponse> CreateSaleAsync(CreateSaleRequest request);
    Task<Sale?> GetSaleByIdAsync(long id);
}
