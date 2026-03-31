using MultiTenant.Api.Domain.Common;



namespace MultiTenant.Api.Domain.Entities;



/// <summary>When <see cref="Workflow.ApplyToAllBusinessUnits"/> is false, the workflow applies only to these BUs.</summary>

public sealed class WorkflowBusinessUnitScope : ITenantEntity

{

    public Guid WorkflowBusinessUnitScopeId { get; set; }

    public Guid TenantId { get; set; }



    public Guid WorkflowId { get; set; }

    public Workflow Workflow { get; set; } = default!;



    public Guid BusinessUnitId { get; set; }

    public BusinessUnit BusinessUnit { get; set; } = default!;

}


