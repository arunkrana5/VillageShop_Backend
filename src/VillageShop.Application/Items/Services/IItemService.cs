using System.Collections.Generic;
using System.Threading.Tasks;
using VillageShop.Application.Items.DTOs;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Application.Items.Services;

public interface IItemService
{
    Task<PostResponse> CreateAsync(CreateItemRequest request);
    Task<PostResponse> UpdateAsync(UpdateItemRequest request);
    Task<PostResponse> DeleteAsync(long id);
    Task<Item?> GetByIdAsync(long id);
    Task<IEnumerable<Item>> SearchAsync(ItemSearchRequest request);
}
