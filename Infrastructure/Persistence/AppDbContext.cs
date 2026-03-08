using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Domain.Common;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Identity;

namespace MultiTenant.Api.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    // ---- Your tables ----
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Vendor> Vendors => Set<Vendor>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ----- Tenants -----
        builder.Entity<Tenant>(b =>
        {
            b.ToTable("Tenants");
            b.HasKey(x => x.TenantId);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();

            b.HasIndex(x => x.Name);
        });

        // ----- TenantUsers (membership) -----
        builder.Entity<TenantUser>(b =>
        {
            b.ToTable("TenantUsers");
            b.HasKey(x => x.TenantUserId);

            // One user can be in a tenant only once
            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();

            // Useful lookups
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        // ----- Vendors (tenant-scoped example) -----
        builder.Entity<Vendor>(b =>
        {
            b.ToTable("Vendors");
            b.HasKey(x => x.VendorId);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            // Tenant-first indexes (important for performance)
            b.HasIndex(x => new { x.TenantId, x.VendorId });
            b.HasIndex(x => new { x.TenantId, x.Name });
        });

        // ----- Global tenant filters (applies to ALL entities implementing ITenantEntity) -----
        ApplyTenantQueryFilters(builder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // Build filter: e => _tenant.TenantId.HasValue && e.TenantId == _tenant.TenantId.Value
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (!typeof(ITenantEntity).IsAssignableFrom(clrType))
                continue;

            // parameter: (TEntity e) =>
            var parameter = Expression.Parameter(clrType, "e");

            // e.TenantId
            var tenantIdProp = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

            // _tenant.TenantId.HasValue
            var tenantCtx = Expression.Constant(this);
            var tenantField = Expression.Field(tenantCtx, nameof(_tenant));
            var tenantIdNullable = Expression.Property(tenantField, nameof(ITenantContext.TenantId));
            var hasValue = Expression.Property(tenantIdNullable, nameof(Nullable<Guid>.HasValue));

            // _tenant.TenantId.Value
            var value = Expression.Property(tenantIdNullable, nameof(Nullable<Guid>.Value));

            // e.TenantId == _tenant.TenantId.Value
            var equals = Expression.Equal(tenantIdProp, value);

            // _tenant.TenantId.HasValue && e.TenantId == _tenant.TenantId.Value
            var body = Expression.AndAlso(hasValue, equals);

            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }
}
