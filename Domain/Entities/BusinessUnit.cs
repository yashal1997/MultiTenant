using MultiTenant.Api.Domain.Common;

namespace MultiTenant.Api.Domain.Entities;

public sealed class BusinessUnit : ITenantEntity
{
    public Guid BusinessUnitId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? HeadOfUnitUserId { get; set; }

    public Department? Department { get; set; }

    public string Name { get; set; } = default!;
    public string UnitCode { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
