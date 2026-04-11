namespace MultiTenant.Api.Domain.Entities;

/// <summary>Lifecycle: Draft (editable), Active (in use), Closed (archived).</summary>
public enum BudgetStatus
{
    Draft = 0,
    Active = 1,
    Closed = 2
}
