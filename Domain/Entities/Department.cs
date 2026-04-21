using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

public sealed class Department : ITenantEntity
{
    public Guid DepartmentId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = default!;
    public string DepartmentCode { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? HeadOfDepartmentUserId { get; set; }
    public Guid? PrimaryBusinessUnitId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<BusinessUnit> BusinessUnits { get; set; } = new List<BusinessUnit>();
}
