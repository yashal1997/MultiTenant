namespace MultiTenant.Api.Contracts.GlAccounts;

public sealed record UpdateGlAccountRequest(
    string Code,
    string Name,
    string? Description,
    bool IsActive
);

