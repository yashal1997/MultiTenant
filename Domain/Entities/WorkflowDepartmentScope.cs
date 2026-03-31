using MultiTenant.Api.Domain.Common;



namespace MultiTenant.Api.Domain.Entities;



/// <summary>When <see cref="Workflow.ApplyToAllDepartments"/> is false, the workflow applies only to these departments.</summary>

public sealed class WorkflowDepartmentScope : ITenantEntity

{

    public Guid WorkflowDepartmentScopeId { get; set; }

    public Guid TenantId { get; set; }



    public Guid WorkflowId { get; set; }

    public Workflow Workflow { get; set; } = default!;



    public Guid DepartmentId { get; set; }

    public Department Department { get; set; } = default!;

}


