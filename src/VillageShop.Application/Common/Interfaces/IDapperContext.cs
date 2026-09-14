using System.Data;

namespace VillageShop.Application.Common.Interfaces;

public interface IDapperContext
{
    IDbConnection CreateConnection();
}
