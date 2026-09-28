using System.Collections.Generic;
using System.Threading.Tasks;
using VillageShop.Application.ItemMasters.DTOs;
using VillageShop.Common.Models;
using VillageShop.Domain.Entities;

namespace VillageShop.Application.ItemMasters.Services;

public interface IItemMasterService
{
    Task<PostResponse> CreateAsync(CreateItemMasterRequest request);
    Task<PostResponse> UpdateAsync(UpdateItemMasterRequest request);
    Task<PostResponse> DeleteAsync(long id);
    Task<ItemMaster?> GetByIdAsync(long id);
    Task<IEnumerable<ItemMaster>> SearchAsync(ItemMasterSearchRequest request);
}
