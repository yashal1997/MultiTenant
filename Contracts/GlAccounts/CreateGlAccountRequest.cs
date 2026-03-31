namespace MultiTenant.Api.Contracts.GlAccounts;

public sealed record CreateGlAccountRequest(
    string Code,
    string Name,
    string? Description
);

