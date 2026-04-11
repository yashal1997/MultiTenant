using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.Budgets;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/budgets")]
public sealed class BudgetsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public BudgetsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BudgetDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;
        var name = request.Name.Trim();

        var headerErr = ValidateBudgetHeader(
            name, request.FiscalYear, request.StartDateUtc, request.EndDateUtc,
            request.TotalAmount, request.CurrencyCode, request.Status, request.Lines.Count);
        if (headerErr is not null)
            return BadRequest(new { message = headerErr });

        var dup = await _db.Budgets.AsNoTracking()
            .AnyAsync(x => x.FiscalYear == request.FiscalYear && x.Name == name);
        if (dup)
            return Conflict(new { message = "A budget with this name already exists for the fiscal year." });

        var lineErr = await ValidateBudgetLinesAsync(tenantId, request.Lines);
        if (lineErr is not null)
            return BadRequest(new { message = lineErr });

        var sum = request.Lines.Sum(x => x.AllocatedAmount);
        var capErr = ValidateTotalCap(request.TotalAmount, sum);
        if (capErr is not null)
            return BadRequest(new { message = capErr });

        var budget = new Budget
        {
            BudgetId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = request.Description?.Trim(),
            FiscalYear = request.FiscalYear,
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            CurrencyCode = NormalizeCurrency(request.CurrencyCode) ?? "USD",
            Status = request.Status,
            TotalAmount = request.TotalAmount,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        AddLinesToBudget(budget, request.Lines);

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { budgetId = budget.BudgetId }, await ToDetailResponseAsync(budget.BudgetId));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BudgetListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int? fiscalYear = null,
        [FromQuery] BudgetStatus? status = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var q = _db.Budgets.AsNoTracking().AsQueryable();

        if (fiscalYear.HasValue)
            q = q.Where(x => x.FiscalYear == fiscalYear.Value);
        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);
        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || (x.Description != null && x.Description.Contains(s)));
        }

        var budgets = await q
            .OrderByDescending(x => x.FiscalYear)
            .ThenBy(x => x.Name)
            .ToListAsync();

        if (budgets.Count == 0)
            return Ok(new List<BudgetListItemResponse>());

        var ids = budgets.Select(x => x.BudgetId).ToList();
        var sums = await _db.BudgetLines.AsNoTracking()
            .Where(l => ids.Contains(l.BudgetId))
            .GroupBy(l => l.BudgetId)
            .Select(g => new { BudgetId = g.Key, Sum = g.Sum(x => x.AllocatedAmount) })
            .ToDictionaryAsync(x => x.BudgetId, x => x.Sum);

        var rows = budgets.Select(b => new BudgetListItemResponse(
            b.BudgetId,
            b.Name,
            b.FiscalYear,
            b.StartDateUtc,
            b.EndDateUtc,
            b.Status,
            b.CurrencyCode,
            b.TotalAmount,
            sums.GetValueOrDefault(b.BudgetId, 0),
            b.IsActive,
            b.CreatedAtUtc,
            b.UpdatedAtUtc)).ToList();

        return Ok(rows);
    }

    [HttpGet("{budgetId:guid}")]
    [ProducesResponseType(typeof(BudgetDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid budgetId)
    {
        var dto = await ToDetailResponseAsync(budgetId);
        if (dto is null)
            return NotFound();
        return Ok(dto);
    }

    [HttpPut("{budgetId:guid}")]
    [ProducesResponseType(typeof(BudgetDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid budgetId, [FromBody] UpdateBudgetRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;
        var name = request.Name.Trim();

        var headerErr = ValidateBudgetHeader(
            name, request.FiscalYear, request.StartDateUtc, request.EndDateUtc,
            request.TotalAmount, request.CurrencyCode, request.Status, request.Lines.Count);
        if (headerErr is not null)
            return BadRequest(new { message = headerErr });

        var budget = await _db.Budgets.FirstOrDefaultAsync(x => x.BudgetId == budgetId);
        if (budget is null)
            return NotFound();

        if (!AllowsStatusChange(budget.Status, request.Status))
            return BadRequest(new { message = $"Cannot change status from {budget.Status} to {request.Status}." });

        if (request.Status == BudgetStatus.Active && request.Lines.Count == 0)
            return BadRequest(new { message = "An active budget must have at least one line." });

        var dup = await _db.Budgets.AsNoTracking()
            .AnyAsync(x => x.BudgetId != budgetId && x.FiscalYear == request.FiscalYear && x.Name == name);
        if (dup)
            return Conflict(new { message = "A budget with this name already exists for the fiscal year." });

        var lineErr = await ValidateBudgetLinesAsync(tenantId, request.Lines);
        if (lineErr is not null)
            return BadRequest(new { message = lineErr });

        var sum = request.Lines.Sum(x => x.AllocatedAmount);
        var capErr = ValidateTotalCap(request.TotalAmount, sum);
        if (capErr is not null)
            return BadRequest(new { message = capErr });

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            budget.Name = name;
            budget.Description = request.Description?.Trim();
            budget.FiscalYear = request.FiscalYear;
            budget.StartDateUtc = request.StartDateUtc;
            budget.EndDateUtc = request.EndDateUtc;
            var cur = NormalizeCurrency(request.CurrencyCode);
            budget.CurrencyCode = cur ?? budget.CurrencyCode;
            budget.Status = request.Status;
            budget.TotalAmount = request.TotalAmount;
            budget.IsActive = request.IsActive;
            budget.UpdatedAtUtc = DateTime.UtcNow;

            await _db.BudgetLines.Where(x => x.BudgetId == budgetId).ExecuteDeleteAsync();
            AddLinesToBudget(budget, request.Lines);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var dto = await ToDetailResponseAsync(budgetId);
        return Ok(dto);
    }

    [HttpDelete("{budgetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid budgetId)
    {
        var budget = await _db.Budgets.FirstOrDefaultAsync(x => x.BudgetId == budgetId);
        if (budget is null)
            return NotFound();

        budget.IsActive = false;
        budget.Status = BudgetStatus.Closed;
        budget.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValidateBudgetHeader(
        string name, int fiscalYear, DateTime start, DateTime end,
        decimal? totalAmount, string? currencyCode, BudgetStatus status, int lineCount)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Budget name is required.";
        if (fiscalYear is < 2000 or > 2100)
            return "FiscalYear must be between 2000 and 2100.";
        if (start.Date > end.Date)
            return "StartDateUtc must be on or before EndDateUtc.";
        if (totalAmount is < 0)
            return "TotalAmount cannot be negative.";
        if (!string.IsNullOrWhiteSpace(currencyCode) && currencyCode.Trim().Length != 3)
            return "CurrencyCode must be a 3-letter ISO 4217 code when provided.";
        if (status == BudgetStatus.Active && lineCount == 0)
            return "An active budget must have at least one line.";
        return null;
    }

    private static string? ValidateTotalCap(decimal? totalAmount, decimal sum)
    {
        if (!totalAmount.HasValue)
            return null;
        if (sum > totalAmount.Value)
            return $"Sum of line allocations ({sum}) exceeds TotalAmount ({totalAmount.Value}).";
        return null;
    }

    private static bool AllowsStatusChange(BudgetStatus from, BudgetStatus to)
    {
        if (from == to)
            return true;
        if (from == BudgetStatus.Closed)
            return false;
        if (from == BudgetStatus.Active && to == BudgetStatus.Draft)
            return false;
        return true;
    }

    private static string? NormalizeCurrency(string? c)
    {
        if (string.IsNullOrWhiteSpace(c))
            return null;
        return c.Trim().ToUpperInvariant();
    }

    private static void AddLinesToBudget(Budget budget, IReadOnlyList<BudgetLineInput> lines)
    {
        var order = 0;
        foreach (var line in lines)
        {
            order++;
            budget.Lines.Add(new BudgetLine
            {
                BudgetLineId = Guid.NewGuid(),
                BudgetId = budget.BudgetId,
                SequenceOrder = order,
                DepartmentId = line.DepartmentId,
                BusinessUnitId = line.BusinessUnitId,
                ExpenseCategoryId = line.ExpenseCategoryId,
                GlAccountId = line.GlAccountId,
                AllocatedAmount = line.AllocatedAmount,
                Notes = line.Notes?.Trim()
            });
        }
    }

    private async Task<string?> ValidateBudgetLinesAsync(Guid tenantId, IReadOnlyList<BudgetLineInput> lines)
    {
        foreach (var line in lines)
        {
            if (line.AllocatedAmount <= 0)
                return "Each line must have AllocatedAmount greater than zero.";

            var hasScope = line.DepartmentId.HasValue || line.BusinessUnitId.HasValue ||
                           line.ExpenseCategoryId.HasValue || line.GlAccountId.HasValue;
            if (!hasScope)
                return "Each line must set at least one of: departmentId, businessUnitId, expenseCategoryId, glAccountId.";

            if (line.DepartmentId.HasValue)
            {
                var ok = await _db.Departments.AsNoTracking()
                    .AnyAsync(x => x.DepartmentId == line.DepartmentId.Value && x.TenantId == tenantId && x.IsActive);
                if (!ok)
                    return "One or more department ids are invalid or inactive.";
            }

            if (line.BusinessUnitId.HasValue)
            {
                var bu = await _db.BusinessUnits.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BusinessUnitId == line.BusinessUnitId.Value && x.TenantId == tenantId && x.IsActive);
                if (bu is null)
                    return "One or more business unit ids are invalid or inactive.";
                if (line.DepartmentId.HasValue && bu.DepartmentId != line.DepartmentId.Value)
                    return "Business unit must belong to the selected department when both are set.";
            }

            if (line.ExpenseCategoryId.HasValue)
            {
                var ok = await _db.ExpenseCategories.AsNoTracking()
                    .AnyAsync(x => x.ExpenseCategoryId == line.ExpenseCategoryId.Value && x.TenantId == tenantId && x.IsActive);
                if (!ok)
                    return "One or more expense category ids are invalid or inactive.";
            }

            if (line.GlAccountId.HasValue)
            {
                var ok = await _db.GlAccounts.AsNoTracking()
                    .AnyAsync(x => x.GlAccountId == line.GlAccountId.Value && x.TenantId == tenantId && x.IsActive);
                if (!ok)
                    return "One or more GL account ids are invalid or inactive.";
            }
        }

        return null;
    }

    private async Task<BudgetDetailResponse?> ToDetailResponseAsync(Guid budgetId)
    {
        var b = await _db.Budgets.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(x => x.Department)
            .Include(x => x.Lines).ThenInclude(x => x.BusinessUnit)
            .Include(x => x.Lines).ThenInclude(x => x.ExpenseCategory)
            .Include(x => x.Lines).ThenInclude(x => x.GlAccount)
            .FirstOrDefaultAsync(x => x.BudgetId == budgetId);

        if (b is null)
            return null;

        var lineDtos = b.Lines.OrderBy(x => x.SequenceOrder).Select(l => new BudgetLineResponse(
            l.BudgetLineId,
            l.SequenceOrder,
            l.DepartmentId,
            l.Department?.Name,
            l.BusinessUnitId,
            l.BusinessUnit?.Name,
            l.ExpenseCategoryId,
            l.ExpenseCategory?.Name,
            l.GlAccountId,
            l.GlAccount?.Code,
            l.GlAccount?.Name,
            l.AllocatedAmount,
            l.Notes)).ToList();

        var allocated = lineDtos.Sum(x => x.AllocatedAmount);

        return new BudgetDetailResponse(
            b.BudgetId,
            b.Name,
            b.Description,
            b.FiscalYear,
            b.StartDateUtc,
            b.EndDateUtc,
            b.Status,
            b.CurrencyCode,
            b.TotalAmount,
            allocated,
            b.IsActive,
            lineDtos,
            b.CreatedAtUtc,
            b.UpdatedAtUtc);
    }
}
