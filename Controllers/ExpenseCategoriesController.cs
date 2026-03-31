using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Contracts.ExpenseCategories;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/expense-categories")]
public sealed class ExpenseCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExpenseCategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseCategoryRequest request)
    {
        var name = request.Name.Trim();
        var glCode = request.GlCode.Trim();

        var exists = await _db.ExpenseCategories.AsNoTracking()
            .AnyAsync(x => x.Name == name);

        if (exists)
            return Conflict(new { message = "Expense category name already exists." });

        var gl = await _db.GlAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == glCode && x.IsActive);

        if (gl is null)
            return BadRequest(new { message = "Provided GL code is invalid or inactive." });

        var entity = new ExpenseCategory
        {
            ExpenseCategoryId = Guid.NewGuid(),
            TenantId = gl.TenantId,
            Name = name,
            Description = request.Description?.Trim(),
            GlAccountId = gl.GlAccountId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ExpenseCategories.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { expenseCategoryId = entity.ExpenseCategoryId }, ToResponse(entity, gl));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ExpenseCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool? isActive = null, [FromQuery] string? search = null)
    {
        var q = _db.ExpenseCategories
            .AsNoTracking()
            .Include(x => x.GlAccount)
            .AsQueryable();

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x =>
                x.Name.Contains(s) ||
                x.GlAccount.Code.Contains(s) ||
                x.GlAccount.Name.Contains(s));
        }

        var rows = await q
            .OrderBy(x => x.Name)
            .Select(x => new ExpenseCategoryResponse(
                x.ExpenseCategoryId,
                x.Name,
                x.Description,
                x.IsActive,
                x.GlAccountId,
                x.GlAccount.Code,
                x.GlAccount.Name,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            ))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("{expenseCategoryId:guid}")]
    [ProducesResponseType(typeof(ExpenseCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid expenseCategoryId)
    {
        var entity = await _db.ExpenseCategories.AsNoTracking()
            .Include(x => x.GlAccount)
            .FirstOrDefaultAsync(x => x.ExpenseCategoryId == expenseCategoryId);

        if (entity is null)
            return NotFound();

        return Ok(ToResponse(entity, entity.GlAccount));
    }

    [HttpPut("{expenseCategoryId:guid}")]
    [ProducesResponseType(typeof(ExpenseCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid expenseCategoryId, [FromBody] UpdateExpenseCategoryRequest request)
    {
        var entity = await _db.ExpenseCategories
            .Include(x => x.GlAccount)
            .FirstOrDefaultAsync(x => x.ExpenseCategoryId == expenseCategoryId);

        if (entity is null)
            return NotFound();

        var newName = request.Name.Trim();
        var glCode = request.GlCode.Trim();

        var exists = await _db.ExpenseCategories.AsNoTracking()
            .AnyAsync(x => x.ExpenseCategoryId != expenseCategoryId && x.Name == newName);

        if (exists)
            return Conflict(new { message = "Expense category name already exists." });

        var gl = await _db.GlAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == glCode && x.IsActive);

        if (gl is null)
            return BadRequest(new { message = "Provided GL code is invalid or inactive." });

        entity.Name = newName;
        entity.Description = request.Description?.Trim();
        entity.GlAccountId = gl.GlAccountId;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(entity, gl));
    }

    [HttpDelete("{expenseCategoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid expenseCategoryId)
    {
        var entity = await _db.ExpenseCategories
            .FirstOrDefaultAsync(x => x.ExpenseCategoryId == expenseCategoryId);

        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ExpenseCategoryResponse ToResponse(ExpenseCategory x, GlAccount gl) => new(
        x.ExpenseCategoryId,
        x.Name,
        x.Description,
        x.IsActive,
        x.GlAccountId,
        gl.Code,
        gl.Name,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    );
}

