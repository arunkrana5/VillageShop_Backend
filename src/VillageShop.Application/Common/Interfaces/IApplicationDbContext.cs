using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Domain.Entities;

namespace VillageShop.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<TenantConfiguration> TenantConfigurations { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<UdhaarLedger> UdhaarLedgers { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<SyncQueue> SyncQueues { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
