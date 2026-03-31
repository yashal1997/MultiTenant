using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MultiTenant.Api.Middleware;

public sealed class TenantResolverMiddleware : IMiddleware
{
    private readonly ITenantContext _tenantContext;
    private readonly AppDbContext _db;
    private static bool IsPlatformAdmin(HttpContext context)
    {
        return string.Equals(
            context.User.FindFirst("is_platform_admin")?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
    public TenantResolverMiddleware(
        ITenantContext tenantContext,
        AppDbContext db)
    {
        _tenantContext = tenantContext;
        _db = db;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var endpoint = context.GetEndpoint();

        // Skip tenant enforcement for explicitly marked endpoints
        if (endpoint?.Metadata?.GetMetadata<SkipTenantResolutionAttribute>() != null ||
            endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            await next(context);
            return;
        }

        // Require auth first
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        // Try resolve tenant from token (if present)
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        var hasTenant = Guid.TryParse(tenantClaim, out var tenantId);

        // Platform admin:
        // - can access platform endpoints without tenant
        // - if tenant_id is present, set tenant context for tenant endpoints
        if (IsPlatformAdmin(context))
        {
            if (hasTenant)
            {
                var tenantOk = await _db.Tenants.AsNoTracking()
                    .AnyAsync(t => t.TenantId == tenantId && t.Status == "ACTIVE");

                if (!tenantOk)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Invalid or inactive tenant");
                    return;
                }

                _tenantContext.SetTenant(tenantId);
            }

            await next(context);
            return;
        }

        // Non-platform users must have tenant_id
        if (!hasTenant)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Tenant not resolved");
            return;
        }

        // Validate tenant
        var tenantIsActive = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.TenantId == tenantId && t.Status == "ACTIVE");

        if (!tenantIsActive)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid or inactive tenant");
            return;
        }

        // Validate membership
        var userId =
            context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var uid))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid user");
            return;
        }

        var isMember = await _db.TenantUsers.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.UserId == uid && x.IsActive);

        if (!isMember)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Not a member of tenant");
            return;
        }

        _tenantContext.SetTenant(tenantId);
        await next(context);
    }


}
