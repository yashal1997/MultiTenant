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
    public DbSet<GlAccount> GlAccounts => Set<GlAccount>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowBusinessUnitScope> WorkflowBusinessUnitScopes => Set<WorkflowBusinessUnitScope>();
    public DbSet<WorkflowDepartmentScope> WorkflowDepartmentScopes => Set<WorkflowDepartmentScope>();
    public DbSet<WorkflowExpenseCategoryScope> WorkflowExpenseCategoryScopes => Set<WorkflowExpenseCategoryScope>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<ExpenseRequest> ExpenseRequests => Set<ExpenseRequest>();
    public DbSet<ExpenseRequestLine> ExpenseRequestLines => Set<ExpenseRequestLine>();
    public DbSet<ExpenseRequestApproval> ExpenseRequestApprovals => Set<ExpenseRequestApproval>();
    public DbSet<ExpenseRequestSequence> ExpenseRequestSequences => Set<ExpenseRequestSequence>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<TenantGeneralSetting> TenantGeneralSettings => Set<TenantGeneralSetting>();

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
            b.HasIndex(x => new { x.TenantId, x.DepartmentId });
            b.HasIndex(x => new { x.TenantId, x.BusinessUnitId });

            b.Property(x => x.JobTitle).HasMaxLength(200);
            b.Property(x => x.EmployeeId).HasMaxLength(100);

            b.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.BusinessUnit)
                .WithMany()
                .HasForeignKey(x => x.BusinessUnitId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.LineManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ----- Vendors (tenant-scoped) -----
        builder.Entity<Vendor>(b =>
        {
            b.ToTable("Vendors");
            b.HasKey(x => x.VendorId);

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.LegalName).HasMaxLength(200);
            b.Property(x => x.Category).HasMaxLength(100);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Email).HasMaxLength(320);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Website).HasMaxLength(500);
            b.Property(x => x.TaxIdentifier).HasMaxLength(80);
            b.Property(x => x.DefaultCurrency).HasMaxLength(3);
            b.Property(x => x.PaymentMethod).HasMaxLength(100);
            b.Property(x => x.AddressLine1).HasMaxLength(200);
            b.Property(x => x.AddressLine2).HasMaxLength(200);
            b.Property(x => x.City).HasMaxLength(120);
            b.Property(x => x.StateRegion).HasMaxLength(120);
            b.Property(x => x.PostalCode).HasMaxLength(30);
            b.Property(x => x.Country).HasMaxLength(100);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasOne(x => x.DefaultGlAccount)
                .WithMany()
                .HasForeignKey(x => x.DefaultGlAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.TenantId, x.VendorId });
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Name });
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        // ----- GL Accounts (tenant-scoped) -----
        builder.Entity<GlAccount>(b =>
        {
            b.ToTable("GlAccounts");
            b.HasKey(x => x.GlAccountId);

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => new { x.TenantId, x.GlAccountId });
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Name });
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        // ----- Expense Categories (tenant-scoped) -----
        builder.Entity<ExpenseCategory>(b =>
        {
            b.ToTable("ExpenseCategories");
            b.HasKey(x => x.ExpenseCategoryId);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.CategoryCode).HasMaxLength(50).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasOne(x => x.GlAccount)
                .WithMany()
                .HasForeignKey(x => x.GlAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.ExpenseCategoryId });
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.CategoryCode }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.GlAccountId });
        });

        // ----- Departments (tenant-scoped) -----
        builder.Entity<Department>(b =>
        {
            b.ToTable("Departments");
            b.HasKey(x => x.DepartmentId);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.DepartmentCode).HasMaxLength(50).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.HeadOfDepartmentUserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne<BusinessUnit>()
                .WithMany()
                .HasForeignKey(x => x.PrimaryBusinessUnitId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.TenantId, x.DepartmentId });
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.DepartmentCode }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        // ----- Business units (tenant-scoped, under department) -----
        builder.Entity<BusinessUnit>(b =>
        {
            b.ToTable("BusinessUnits");
            b.HasKey(x => x.BusinessUnitId);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.UnitCode).HasMaxLength(50).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasOne(x => x.Department)
                .WithMany(x => x.BusinessUnits)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.HeadOfUnitUserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.TenantId, x.BusinessUnitId });
            b.HasIndex(x => new { x.TenantId, x.UnitCode }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        // ----- Budgets (tenant-scoped) -----
        builder.Entity<Budget>(b =>
        {
            b.ToTable("Budgets");
            b.HasKey(x => x.BudgetId);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);

            b.HasOne(x => x.BusinessUnit)
                .WithMany()
                .HasForeignKey(x => x.BusinessUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.Lines)
                .WithOne(x => x.Budget)
                .HasForeignKey(x => x.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.BusinessUnitId });
            b.HasIndex(x => new { x.TenantId, x.BudgetId });
            b.HasIndex(x => new { x.TenantId, x.FiscalYear, x.Name }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.FiscalYear });
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        builder.Entity<BudgetLine>(b =>
        {
            b.ToTable("BudgetLines");
            b.HasKey(x => x.BudgetLineId);

            b.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.BusinessUnit)
                .WithMany()
                .HasForeignKey(x => x.BusinessUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.ExpenseCategory)
                .WithMany()
                .HasForeignKey(x => x.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.GlAccount)
                .WithMany()
                .HasForeignKey(x => x.GlAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.BudgetId);
            b.HasIndex(x => new { x.BudgetId, x.SequenceOrder }).IsUnique();
            b.HasIndex(x => new { x.BudgetId, x.DepartmentId, x.ExpenseCategoryId }).IsUnique();
        });

        // ----- Expense requests (tenant-scoped) -----
        builder.Entity<ExpenseRequest>(b =>
        {
            b.ToTable("ExpenseRequests");
            b.HasKey(x => x.ExpenseRequestId);

            b.Property(x => x.RequestNumber).HasMaxLength(40).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.ExpenseType).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.ProjectId).HasMaxLength(200);
            b.Property(x => x.FundingType).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();

            b.HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.ExpenseCategory)
                .WithMany()
                .HasForeignKey(x => x.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.BusinessUnit)
                .WithMany()
                .HasForeignKey(x => x.BusinessUnitId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.Budget)
                .WithMany()
                .HasForeignKey(x => x.BudgetId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.Workflow)
                .WithMany()
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.Lines)
                .WithOne(x => x.ExpenseRequest)
                .HasForeignKey(x => x.ExpenseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Approvals)
                .WithOne(x => x.ExpenseRequest)
                .HasForeignKey(x => x.ExpenseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.ExpenseRequestId });
            b.HasIndex(x => new { x.TenantId, x.RequestNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.SubmittedByUserId });
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        builder.Entity<ExpenseRequestLine>(b =>
        {
            b.ToTable("ExpenseRequestLines");
            b.HasKey(x => x.ExpenseRequestLineId);

            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 2);

            b.HasOne(x => x.ExpenseRequest)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.ExpenseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.LineExpenseCategory)
                .WithMany()
                .HasForeignKey(x => x.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.GlAccount)
                .WithMany()
                .HasForeignKey(x => x.GlAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.LineVendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.TenantId, x.ExpenseRequestId });
            b.HasIndex(x => new { x.ExpenseRequestId, x.SequenceOrder }).IsUnique();
        });

        builder.Entity<ExpenseRequestApproval>(b =>
        {
            b.ToTable("ExpenseRequestApprovals");
            b.HasKey(x => x.ExpenseRequestApprovalId);

            b.Property(x => x.StepStatus).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Comment).HasMaxLength(2000);

            b.HasOne(x => x.ExpenseRequest)
                .WithMany(x => x.Approvals)
                .HasForeignKey(x => x.ExpenseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.ExpenseRequestId });
            b.HasIndex(x => new { x.ExpenseRequestId, x.StepSequence }).IsUnique();
        });

        builder.Entity<ExpenseRequestSequence>(b =>
        {
            b.ToTable("ExpenseRequestSequences");
            b.HasKey(x => x.ExpenseRequestSequenceId);

            b.HasIndex(x => new { x.TenantId, x.Year }).IsUnique();
        });

        // ----- Workflows (tenant-scoped approval chains) -----
        builder.Entity<Workflow>(b =>
        {
            b.ToTable("Workflows");
            b.HasKey(x => x.WorkflowId);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.ApprovalThresholdAmount).HasPrecision(18, 2);

            b.HasIndex(x => new { x.TenantId, x.WorkflowId });
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        builder.Entity<WorkflowBusinessUnitScope>(b =>
        {
            b.ToTable("WorkflowBusinessUnitScopes");
            b.HasKey(x => x.WorkflowBusinessUnitScopeId);

            b.HasOne(x => x.Workflow)
                .WithMany(x => x.BusinessUnitScopes)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.BusinessUnit)
                .WithMany()
                .HasForeignKey(x => x.BusinessUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.WorkflowId, x.BusinessUnitId }).IsUnique();
        });

        builder.Entity<WorkflowDepartmentScope>(b =>
        {
            b.ToTable("WorkflowDepartmentScopes");
            b.HasKey(x => x.WorkflowDepartmentScopeId);

            b.HasOne(x => x.Workflow)
                .WithMany(x => x.DepartmentScopes)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.WorkflowId, x.DepartmentId }).IsUnique();
        });

        builder.Entity<WorkflowExpenseCategoryScope>(b =>
        {
            b.ToTable("WorkflowExpenseCategoryScopes");
            b.HasKey(x => x.WorkflowExpenseCategoryScopeId);

            b.HasOne(x => x.Workflow)
                .WithMany(x => x.ExpenseCategoryScopes)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.ExpenseCategory)
                .WithMany()
                .HasForeignKey(x => x.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.WorkflowId, x.ExpenseCategoryId }).IsUnique();
        });

        builder.Entity<WorkflowStep>(b =>
        {
            b.ToTable("WorkflowSteps");
            b.HasKey(x => x.WorkflowStepId);

            b.HasOne(x => x.Workflow)
                .WithMany(x => x.Steps)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.WorkflowId, x.Sequence }).IsUnique();
        });

        // ----- Notification settings (tenant-scoped per user) -----
        builder.Entity<NotificationSetting>(b =>
        {
            b.ToTable("NotificationSettings");
            b.HasKey(x => x.NotificationSettingId);

            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ----- Tenant general settings (tenant-scoped single row) -----
        builder.Entity<TenantGeneralSetting>(b =>
        {
            b.ToTable("TenantGeneralSettings");
            b.HasKey(x => x.TenantGeneralSettingId);

            b.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            b.Property(x => x.LegalName).HasMaxLength(200);
            b.Property(x => x.SupportEmail).HasMaxLength(256);
            b.Property(x => x.PhoneNumber).HasMaxLength(50);
            b.Property(x => x.WebsiteUrl).HasMaxLength(300);
            b.Property(x => x.TaxRegistrationNumber).HasMaxLength(100);

            b.Property(x => x.AddressLine1).HasMaxLength(200);
            b.Property(x => x.AddressLine2).HasMaxLength(200);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.StateOrProvince).HasMaxLength(100);
            b.Property(x => x.PostalCode).HasMaxLength(30);
            b.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();

            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            b.Property(x => x.DateFormat).HasMaxLength(30).IsRequired();

            b.HasIndex(x => x.TenantId).IsUnique();

            b.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
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
