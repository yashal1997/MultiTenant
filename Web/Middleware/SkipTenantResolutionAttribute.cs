namespace MultiTenant.Api.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipTenantResolutionAttribute : Attribute
{
}