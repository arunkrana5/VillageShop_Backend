using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VillageShop.Application.Common.Interfaces;
using VillageShop.Domain.Common;
using VillageShop.Domain.Entities;

namespace VillageShop.Infrastructure.EFCore;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentTenantService _currentTenantService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantService currentTenantService)
        : base(options)
    {
        _currentTenantService = currentTenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<TenantConfiguration> TenantConfigurations => Set<TenantConfiguration>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<UdhaarLedger> UdhaarLedgers => Set<UdhaarLedger>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<SyncQueue> SyncQueues => Set<SyncQueue>();

    public long CurrentTenantId => _currentTenantService != null && _currentTenantService.TenantId > 0 ? _currentTenantService.TenantId : 1;
    public bool IsSuperAdmin => _currentTenantService?.IsSuperAdmin ?? false;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enforce V_ table prefix for all VillageShop entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName) && !tableName.StartsWith("V_"))
            {
                entityType.SetTableName("V_" + tableName);
            }
        }

        // Tenant unique index
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.TenantCode)
            .IsUnique();

        // Join table configuration
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId);

        // Money & decimal precision configurations
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.PurchasePrice).HasPrecision(18, 2);
            entity.Property(p => p.SellingPrice).HasPrecision(18, 2);
            entity.Property(p => p.MRP).HasPrecision(18, 2);
            entity.Property(p => p.GSTPercent).HasPrecision(5, 2);
            entity.Property(p => p.OpeningStock).HasPrecision(18, 3);
            entity.Property(p => p.MinimumStock).HasPrecision(18, 3);
            entity.Property(p => p.CurrentStock).HasPrecision(18, 3);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.CreditLimit).HasPrecision(18, 2);
            entity.Property(c => c.OpeningBalance).HasPrecision(18, 2);
            entity.Property(c => c.CurrentBalance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(s => s.OpeningBalance).HasPrecision(18, 2);
            entity.Property(s => s.CurrentBalance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(s => s.SubTotal).HasPrecision(18, 2);
            entity.Property(s => s.TaxAmount).HasPrecision(18, 2);
            entity.Property(s => s.DiscountAmount).HasPrecision(18, 2);
            entity.Property(s => s.TotalAmount).HasPrecision(18, 2);
            entity.Property(s => s.PaidAmount).HasPrecision(18, 2);
            entity.Property(s => s.UdhaarAmount).HasPrecision(18, 2);
            entity.HasIndex(s => s.ClientTransactionId).IsUnique(); // Enforce Idempotency constraint
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(si => si.Quantity).HasPrecision(18, 3);
            entity.Property(si => si.UnitPrice).HasPrecision(18, 2);
            entity.Property(si => si.TaxPercent).HasPrecision(5, 2);
            entity.Property(si => si.TaxAmount).HasPrecision(18, 2);
            entity.Property(si => si.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<UdhaarLedger>(entity =>
        {
            entity.Property(u => u.DebitAmount).HasPrecision(18, 2);
            entity.Property(u => u.CreditAmount).HasPrecision(18, 2);
            entity.Property(u => u.RunningBalance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        // Global Tenant Isolation Query Filters
        modelBuilder.Entity<User>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Role>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<TenantConfiguration>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Product>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Sale>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<SaleItem>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Payment>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<UdhaarLedger>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Expense>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<SyncQueue>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = _currentTenantService.TenantId;
        var currentUserId = _currentTenantService.UserId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is BaseTenantEntity tenantEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    if (tenantEntity.TenantId == 0 && currentTenantId > 0)
                    {
                        tenantEntity.TenantId = currentTenantId;
                    }
                }
            }

            if (entry.Entity is BaseEntity entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedDate = DateTime.UtcNow;
                    entity.CreatedBy = currentUserId;
                    entity.ModifiedDate = DateTime.UtcNow;
                    entity.ModifiedBy = currentUserId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.ModifiedDate = DateTime.UtcNow;
                    entity.ModifiedBy = currentUserId;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
