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
        var name = (request.Name ?? string.Empty).Trim();

        var headerError = await ValidateBudgetHeaderAsync(tenantId, name, request.BusinessUnitId, request.StartDateUtc, request.EndDateUtc);
        if (headerError is not null)
            return BadRequest(new { message = headerError });

        var fiscalYear = request.StartDateUtc.Year;
        var duplicate = await _db.Budgets.AsNoTracking()
            .AnyAsync(x => x.FiscalYear == fiscalYear && x.Name == name);
        if (duplicate)
            return Conflict(new { message = "A budget with this name already exists for the fiscal year." });

        var lineError = await ValidateBudgetLinesAsync(tenantId, request.BusinessUnitId, request.Lines);
        if (lineError is not null)
            return BadRequest(new { message = lineError });

        var budget = new Budget
        {
            BudgetId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = request.Description?.Trim(),
            BusinessUnitId = request.BusinessUnitId,
            FiscalYear = fiscalYear,
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            CurrencyCode = "USD",
            Status = request.IsActive ? BudgetStatus.Active : BudgetStatus.Draft,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        AddLinesToBudget(budget, request.Lines);
        budget.TotalAmount = budget.Lines.Sum(x => x.AllocatedAmount);

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { budgetId = budget.BudgetId }, await ToDetailResponseAsync(budget.BudgetId));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BudgetListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? periodFrom = null,
        [FromQuery] DateTime? periodTo = null)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var query = _db.Budgets.AsNoTracking()
            .Include(x => x.BusinessUnit)
            .Include(x => x.Lines)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(trimmed) ||
                x.BusinessUnit.Name.Contains(trimmed) ||
                (x.Description != null && x.Description.Contains(trimmed)));
        }

        if (periodFrom.HasValue)
            query = query.Where(x => x.EndDateUtc >= periodFrom.Value);

        if (periodTo.HasValue)
            query = query.Where(x => x.StartDateUtc <= periodTo.Value);

        var budgets = await query
            .OrderByDescending(x => x.StartDateUtc)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var rows = budgets.Select(x =>
        {
            var allocatedTotal = x.Lines.Sum(line => line.AllocatedAmount);
            const decimal spentTotal = 0m;
            var remainingTotal = allocatedTotal;

            return new BudgetListItemResponse(
                x.BudgetId,
                x.Name,
                x.BusinessUnitId,
                x.BusinessUnit.Name,
                x.StartDateUtc,
                x.EndDateUtc,
                allocatedTotal,
                spentTotal,
                remainingTotal,
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc);
        }).ToList();

        return Ok(rows);
    }

    [HttpGet("{budgetId:guid}")]
    [ProducesResponseType(typeof(BudgetDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid budgetId)
    {
        var response = await ToDetailResponseAsync(budgetId);
        if (response is null)
            return NotFound();

        return Ok(response);
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
        var name = (request.Name ?? string.Empty).Trim();

        var headerError = await ValidateBudgetHeaderAsync(tenantId, name, request.BusinessUnitId, request.StartDateUtc, request.EndDateUtc);
        if (headerError is not null)
            return BadRequest(new { message = headerError });

        var budget = await _db.Budgets
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.BudgetId == budgetId);
        if (budget is null)
            return NotFound();

        if (budget.Status == BudgetStatus.Closed)
            return BadRequest(new { message = "Closed budgets cannot be modified." });

        var fiscalYear = request.StartDateUtc.Year;
        var duplicate = await _db.Budgets.AsNoTracking()
            .AnyAsync(x => x.BudgetId != budgetId && x.FiscalYear == fiscalYear && x.Name == name);
        if (duplicate)
            return Conflict(new { message = "A budget with this name already exists for the fiscal year." });

        var lineError = await ValidateBudgetLinesAsync(tenantId, request.BusinessUnitId, request.Lines);
        if (lineError is not null)
            return BadRequest(new { message = lineError });

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            budget.Name = name;
            budget.Description = request.Description?.Trim();
            budget.BusinessUnitId = request.BusinessUnitId;
            budget.FiscalYear = fiscalYear;
            budget.StartDateUtc = request.StartDateUtc;
            budget.EndDateUtc = request.EndDateUtc;
            budget.IsActive = request.IsActive;
            budget.Status = request.IsActive ? BudgetStatus.Active : BudgetStatus.Draft;
            budget.UpdatedAtUtc = DateTime.UtcNow;

            await _db.BudgetLines.Where(x => x.BudgetId == budgetId).ExecuteDeleteAsync();
            budget.Lines.Clear();
            AddLinesToBudget(budget, request.Lines);
            budget.TotalAmount = budget.Lines.Sum(x => x.AllocatedAmount);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Ok(await ToDetailResponseAsync(budgetId));
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

    [HttpPost("{budgetId:guid}/bulk-allocate")]
    [ProducesResponseType(typeof(BudgetDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkAllocate([FromRoute] Guid budgetId, [FromBody] BulkBudgetAllocationRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;
        var budget = await _db.Budgets
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.BudgetId == budgetId);
        if (budget is null)
            return NotFound();

        if (budget.Status == BudgetStatus.Closed)
            return BadRequest(new { message = "Closed budgets cannot be modified." });

        if (request.AllocatedAmount <= 0)
            return BadRequest(new { message = "AllocatedAmount must be greater than zero." });
        if (request.DepartmentIds.Count == 0 || request.ExpenseCategoryIds.Count == 0)
            return BadRequest(new { message = "At least one department and one expense category are required." });

        var departmentsError = await ValidateDepartmentScopeAsync(tenantId, budget.BusinessUnitId, request.DepartmentIds);
        if (departmentsError is not null)
            return BadRequest(new { message = departmentsError });

        var categoriesError = await ValidateExpenseCategoriesAsync(tenantId, request.ExpenseCategoryIds);
        if (categoriesError is not null)
            return BadRequest(new { message = categoriesError });

        var nextSequence = budget.Lines.Count == 0 ? 1 : budget.Lines.Max(x => x.SequenceOrder) + 1;

        foreach (var departmentId in request.DepartmentIds.Distinct())
        {
            foreach (var categoryId in request.ExpenseCategoryIds.Distinct())
            {
                var existing = budget.Lines.FirstOrDefault(x => x.DepartmentId == departmentId && x.ExpenseCategoryId == categoryId);
                if (existing is not null)
                {
                    existing.AllocatedAmount += request.AllocatedAmount;
                    continue;
                }

                budget.Lines.Add(new BudgetLine
                {
                    BudgetLineId = Guid.NewGuid(),
                    BudgetId = budget.BudgetId,
                    SequenceOrder = nextSequence++,
                    DepartmentId = departmentId,
                    BusinessUnitId = budget.BusinessUnitId,
                    ExpenseCategoryId = categoryId,
                    AllocatedAmount = request.AllocatedAmount
                });
            }
        }

        budget.TotalAmount = budget.Lines.Sum(x => x.AllocatedAmount);
        budget.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await ToDetailResponseAsync(budgetId));
    }

    [HttpPost("{budgetId:guid}/adjust")]
    [ProducesResponseType(typeof(BudgetDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Adjust([FromRoute] Guid budgetId, [FromBody] AdjustBudgetRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;
        var budget = await _db.Budgets
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.BudgetId == budgetId);
        if (budget is null)
            return NotFound();

        if (budget.Status == BudgetStatus.Closed)
            return BadRequest(new { message = "Closed budgets cannot be modified." });
        if (request.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        var targetDepartmentId = request.ToDepartmentId ?? request.FromDepartmentId;
        var targetCategoryId = request.ToExpenseCategoryId ?? request.FromExpenseCategoryId;

        if (targetDepartmentId == request.FromDepartmentId && targetCategoryId == request.FromExpenseCategoryId)
            return BadRequest(new { message = "Adjustment must move allocation to a different department or category." });

        var departmentsError = await ValidateDepartmentScopeAsync(tenantId, budget.BusinessUnitId, new[] { request.FromDepartmentId, targetDepartmentId });
        if (departmentsError is not null)
            return BadRequest(new { message = departmentsError });

        var categoriesError = await ValidateExpenseCategoriesAsync(tenantId, new[] { request.FromExpenseCategoryId, targetCategoryId });
        if (categoriesError is not null)
            return BadRequest(new { message = categoriesError });

        var source = budget.Lines.FirstOrDefault(x =>
            x.DepartmentId == request.FromDepartmentId &&
            x.ExpenseCategoryId == request.FromExpenseCategoryId);

        if (source is null)
            return BadRequest(new { message = "Source allocation not found." });
        if (source.AllocatedAmount < request.Amount)
            return BadRequest(new { message = "Transfer amount exceeds the source allocation." });

        var target = budget.Lines.FirstOrDefault(x =>
            x.DepartmentId == targetDepartmentId &&
            x.ExpenseCategoryId == targetCategoryId);

        source.AllocatedAmount -= request.Amount;

        if (target is null)
        {
            var nextSequence = budget.Lines.Count == 0 ? 1 : budget.Lines.Max(x => x.SequenceOrder) + 1;
            budget.Lines.Add(new BudgetLine
            {
                BudgetLineId = Guid.NewGuid(),
                BudgetId = budget.BudgetId,
                SequenceOrder = nextSequence,
                DepartmentId = targetDepartmentId,
                BusinessUnitId = budget.BusinessUnitId,
                ExpenseCategoryId = targetCategoryId,
                AllocatedAmount = request.Amount
            });
        }
        else
        {
            target.AllocatedAmount += request.Amount;
        }

        if (source.AllocatedAmount == 0)
        {
            _db.BudgetLines.Remove(source);
        }

        budget.TotalAmount = budget.Lines.Sum(x => x.AllocatedAmount);
        budget.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await ToDetailResponseAsync(budgetId));
    }

    private async Task<string?> ValidateBudgetHeaderAsync(
        Guid tenantId,
        string name,
        Guid businessUnitId,
        DateTime startDateUtc,
        DateTime endDateUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Budget name is required.";
        if (startDateUtc.Date > endDateUtc.Date)
            return "StartDateUtc must be on or before EndDateUtc.";

        var businessUnitExists = await _db.BusinessUnits.AsNoTracking()
            .AnyAsync(x => x.BusinessUnitId == businessUnitId && x.TenantId == tenantId && x.IsActive);
        return businessUnitExists ? null : "BusinessUnitId is invalid or inactive for this tenant.";
    }

    private async Task<string?> ValidateBudgetLinesAsync(Guid tenantId, Guid businessUnitId, IReadOnlyList<BudgetLineInput> lines)
    {
        foreach (var line in lines)
        {
            if (line.DepartmentId == Guid.Empty)
                return "DepartmentId is required for each allocation.";
            if (line.ExpenseCategoryId == Guid.Empty)
                return "ExpenseCategoryId is required for each allocation.";
            if (line.AllocatedAmount <= 0)
                return "Each line must have AllocatedAmount greater than zero.";
        }

        var departmentsError = await ValidateDepartmentScopeAsync(tenantId, businessUnitId, lines.Select(x => x.DepartmentId));
        if (departmentsError is not null)
            return departmentsError;

        var categoriesError = await ValidateExpenseCategoriesAsync(tenantId, lines.Select(x => x.ExpenseCategoryId));
        if (categoriesError is not null)
            return categoriesError;

        var glIds = lines.Where(x => x.GlAccountId.HasValue).Select(x => x.GlAccountId!.Value).Distinct().ToList();
        if (glIds.Count > 0)
        {
            var validGlIds = await _db.GlAccounts.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive && glIds.Contains(x.GlAccountId))
                .Select(x => x.GlAccountId)
                .ToListAsync();
            if (validGlIds.Count != glIds.Count)
                return "One or more GL account ids are invalid or inactive.";
        }

        var duplicates = lines
            .GroupBy(x => new { x.DepartmentId, x.ExpenseCategoryId })
            .Any(group => group.Count() > 1);
        if (duplicates)
            return "Duplicate department and expense category combinations are not allowed.";

        return null;
    }

    private async Task<string?> ValidateDepartmentScopeAsync(Guid tenantId, Guid businessUnitId, IEnumerable<Guid> departmentIds)
    {
        var ids = departmentIds.Distinct().ToList();
        if (ids.Count == 0)
            return null;

        var businessUnitDepartmentId = await _db.BusinessUnits.AsNoTracking()
            .Where(x => x.BusinessUnitId == businessUnitId && x.TenantId == tenantId && x.IsActive)
            .Select(x => x.DepartmentId)
            .FirstOrDefaultAsync();

        var validIds = await _db.Departments.AsNoTracking()
            .Where(x =>
                ids.Contains(x.DepartmentId) &&
                x.TenantId == tenantId &&
                x.IsActive &&
                (
                    x.PrimaryBusinessUnitId == businessUnitId ||
                    (businessUnitDepartmentId.HasValue && x.DepartmentId == businessUnitDepartmentId.Value)
                ))
            .Select(x => x.DepartmentId)
            .ToListAsync();

        return validIds.Count == ids.Count
            ? null
            : "Departments must be active and belong to the selected business unit.";
    }

    private async Task<string?> ValidateExpenseCategoriesAsync(Guid tenantId, IEnumerable<Guid> expenseCategoryIds)
    {
        var ids = expenseCategoryIds.Distinct().ToList();
        if (ids.Count == 0)
            return null;

        var validIds = await _db.ExpenseCategories.AsNoTracking()
            .Where(x => ids.Contains(x.ExpenseCategoryId) && x.TenantId == tenantId && x.IsActive)
            .Select(x => x.ExpenseCategoryId)
            .ToListAsync();

        return validIds.Count == ids.Count
            ? null
            : "One or more expense category ids are invalid or inactive.";
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
                BusinessUnitId = budget.BusinessUnitId,
                ExpenseCategoryId = line.ExpenseCategoryId,
                GlAccountId = line.GlAccountId,
                AllocatedAmount = line.AllocatedAmount,
                Notes = line.Notes?.Trim()
            });
        }
    }

    private async Task<BudgetDetailResponse?> ToDetailResponseAsync(Guid budgetId)
    {
        var budget = await _db.Budgets.AsNoTracking()
            .Include(x => x.BusinessUnit)
            .Include(x => x.Lines).ThenInclude(x => x.Department)
            .Include(x => x.Lines).ThenInclude(x => x.ExpenseCategory)
            .Include(x => x.Lines).ThenInclude(x => x.GlAccount)
            .FirstOrDefaultAsync(x => x.BudgetId == budgetId);

        if (budget is null)
            return null;

        const decimal spentTotal = 0m;
        var allocationRows = budget.Lines
            .OrderBy(x => x.SequenceOrder)
            .Select(x => new BudgetAllocationRowResponse(
                x.BudgetLineId,
                x.SequenceOrder,
                x.DepartmentId,
                x.Department.Name,
                x.ExpenseCategoryId,
                x.ExpenseCategory.Name,
                x.GlAccountId,
                x.GlAccount?.Code,
                x.GlAccount?.Name,
                x.AllocatedAmount,
                0m,
                x.AllocatedAmount,
                x.Notes))
            .ToList();

        var departmentAllocations = allocationRows
            .GroupBy(x => new { x.DepartmentId, x.DepartmentName })
            .Select(group =>
            {
                var allocatedTotal = group.Sum(x => x.AllocatedAmount);
                return new BudgetDepartmentAllocationResponse(
                    group.Key.DepartmentId,
                    group.Key.DepartmentName,
                    allocatedTotal,
                    0m,
                    allocatedTotal,
                    group.ToList());
            })
            .OrderBy(x => x.DepartmentName)
            .ToList();

        var allocatedTotal = allocationRows.Sum(x => x.AllocatedAmount);

        return new BudgetDetailResponse(
            budget.BudgetId,
            budget.Name,
            budget.Description,
            budget.BusinessUnitId,
            budget.BusinessUnit.Name,
            budget.FiscalYear,
            budget.StartDateUtc,
            budget.EndDateUtc,
            allocatedTotal,
            spentTotal,
            allocatedTotal,
            budget.IsActive,
            departmentAllocations,
            budget.CreatedAtUtc,
            budget.UpdatedAtUtc);
    }
}
