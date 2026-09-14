using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using VillageShop.Application.Common.Interfaces;

namespace VillageShop.Infrastructure.Dapper;

public class DapperContext : IDapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=VillageShopDb;Trusted_Connection=True;MultipleActiveResultSets=true";
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
