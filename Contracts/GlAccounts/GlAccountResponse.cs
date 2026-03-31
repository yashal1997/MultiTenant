namespace MultiTenant.Api.Contracts.GlAccounts;

public sealed record GlAccountResponse(
    Guid GlAccountId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

