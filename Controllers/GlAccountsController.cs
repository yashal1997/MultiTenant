using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.GlAccounts;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/gl-accounts")]
public sealed class GlAccountsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GlAccountsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPost]
    [ProducesResponseType(typeof(GlAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateGlAccountRequest request)
    {
        var tenantId = _tenant.TenantId!.Value;
        var code = request.Code.Trim();
        var name = request.Name.Trim();

        var codeExists = await _db.GlAccounts.AsNoTracking()
            .AnyAsync(x => x.Code == code);

        if (codeExists)
            return Conflict(new { message = "GL account code already exists." });

        var entity = new GlAccount
        {
            GlAccountId = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.GlAccounts.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { glAccountId = entity.GlAccountId }, ToResponse(entity));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<GlAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool? isActive = null, [FromQuery] string? search = null)
    {
        var q = _db.GlAccounts.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Code.Contains(s) || x.Name.Contains(s));
        }

        var rows = await q
            .OrderBy(x => x.Code)
            .Select(x => ToResponse(x))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("{glAccountId:guid}")]
    [ProducesResponseType(typeof(GlAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid glAccountId)
    {
        var entity = await _db.GlAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GlAccountId == glAccountId);

        if (entity is null)
            return NotFound();

        return Ok(ToResponse(entity));
    }

    [HttpPut("{glAccountId:guid}")]
    [ProducesResponseType(typeof(GlAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid glAccountId, [FromBody] UpdateGlAccountRequest request)
    {
        var entity = await _db.GlAccounts
            .FirstOrDefaultAsync(x => x.GlAccountId == glAccountId);

        if (entity is null)
            return NotFound();

        var newCode = request.Code.Trim();
        var newName = request.Name.Trim();

        var codeExists = await _db.GlAccounts.AsNoTracking()
            .AnyAsync(x => x.GlAccountId != glAccountId && x.Code == newCode);

        if (codeExists)
            return Conflict(new { message = "GL account code already exists." });

        entity.Code = newCode;
        entity.Name = newName;
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(entity));
    }

    [HttpDelete("{glAccountId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid glAccountId)
    {
        var entity = await _db.GlAccounts
            .FirstOrDefaultAsync(x => x.GlAccountId == glAccountId);

        if (entity is null)
            return NotFound();

        // Safer than a hard delete for accounting data.
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static GlAccountResponse ToResponse(GlAccount x) => new(
        x.GlAccountId,
        x.Code,
        x.Name,
        x.Description,
        x.IsActive,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    );
}

