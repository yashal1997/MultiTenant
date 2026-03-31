using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.Workflows;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Infrastructure.Persistence;

namespace MultiTenant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public WorkflowsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkflowDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;
        var name = request.Name.Trim();

        var dup = await _db.Workflows.AsNoTracking().AnyAsync(x => x.Name == name);
        if (dup)
            return Conflict(new { message = "Workflow name already exists." });

        var stepError = await ValidateStepInputsAsync(tenantId, request.Steps);
        if (stepError is not null)
            return BadRequest(new { message = stepError });

        var thresholdError = ValidateThreshold(request.ApprovalThresholdAmount);
        if (thresholdError is not null)
            return BadRequest(new { message = thresholdError });

        var scopeError = await ValidateScopeAsync(
            tenantId,
            request.ApplyToAllBusinessUnits,
            request.BusinessUnitIds,
            request.ApplyToAllDepartments,
            request.DepartmentIds,
            request.ApplyToAllExpenseCategories,
            request.ExpenseCategoryIds);
        if (scopeError is not null)
            return BadRequest(new { message = scopeError });

        var (buIds, deptIds, catIds) = NormalizeScopeIds(
            request.ApplyToAllBusinessUnits, request.BusinessUnitIds,
            request.ApplyToAllDepartments, request.DepartmentIds,
            request.ApplyToAllExpenseCategories, request.ExpenseCategoryIds);

        var wf = new Workflow
        {
            WorkflowId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = request.Description?.Trim(),
            ApplyToAllBusinessUnits = request.ApplyToAllBusinessUnits,
            ApplyToAllDepartments = request.ApplyToAllDepartments,
            ApplyToAllExpenseCategories = request.ApplyToAllExpenseCategories,
            ApprovalThresholdAmount = request.ApprovalThresholdAmount,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        AddScopeRows(wf, buIds, deptIds, catIds);

        foreach (var s in request.Steps.OrderBy(x => x.Sequence))
        {
            wf.Steps.Add(new WorkflowStep
            {
                WorkflowStepId = Guid.NewGuid(),
                WorkflowId = wf.WorkflowId,
                Sequence = s.Sequence,
                ApproverUserId = s.ApproverUserId
            });
        }

        _db.Workflows.Add(wf);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { workflowId = wf.WorkflowId }, await ToDetailResponseAsync(wf.WorkflowId));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<WorkflowListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var q = _db.Workflows.AsNoTracking().AsQueryable();

        if (departmentId.HasValue)
        {
            q = q.Where(x =>
                x.ApplyToAllDepartments ||
                x.DepartmentScopes.Any(d => d.DepartmentId == departmentId.Value));
        }

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || (x.Description != null && x.Description.Contains(s)));
        }

        var rows = await q
            .OrderBy(x => x.Name)
            .Select(x => new WorkflowListItemResponse(
                x.WorkflowId,
                x.Name,
                x.Description,
                x.ApplyToAllBusinessUnits,
                x.ApplyToAllDepartments,
                x.ApplyToAllExpenseCategories,
                x.BusinessUnitScopes.Count,
                x.DepartmentScopes.Count,
                x.ExpenseCategoryScopes.Count,
                x.ApprovalThresholdAmount,
                x.IsActive,
                x.Steps.Count,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("{workflowId:guid}")]
    [ProducesResponseType(typeof(WorkflowDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid workflowId)
    {
        var wf = await LoadWorkflowForDetailAsync(workflowId);
        if (wf is null)
            return NotFound();

        return Ok(await ToDetailResponseAsync(wf));
    }

    /// <summary>
    /// Evaluate scope + threshold for a hypothetical expense (for testing). Omit optional scope query params when dimensions are "apply to all".
    /// </summary>
    [HttpGet("{workflowId:guid}/preview-approval")]
    [ProducesResponseType(typeof(WorkflowApprovalEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewApproval(
        [FromRoute] Guid workflowId,
        [FromQuery] decimal amount,
        [FromQuery] Guid? businessUnitId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? expenseCategoryId = null)
    {
        var wf = await LoadWorkflowForDetailAsync(workflowId);
        if (wf is null)
            return NotFound();

        var appliesToScope = wf.AppliesToScope(businessUnitId, departmentId, expenseCategoryId);
        var requiresApprovalForAmount = wf.RequiresApprovalForAmount(amount);
        var shouldRun = wf.ShouldRunApprovalChain(amount, businessUnitId, departmentId, expenseCategoryId);

        string message;
        if (!appliesToScope)
            message = "Scope does not match — this workflow would not apply (try passing BU / dept / category ids that match the workflow scope).";
        else if (!requiresApprovalForAmount)
            message = $"Scope matches; amount {amount} is strictly below threshold {wf.ApprovalThresholdAmount} — skip approval chain.";
        else if (!wf.ApprovalThresholdAmount.HasValue)
            message = "Scope matches; no threshold is set — use the approval chain for this workflow when scope matches.";
        else
            message = $"Scope matches; amount {amount} is at or above threshold {wf.ApprovalThresholdAmount} — run approval chain.";

        return Ok(new WorkflowApprovalEvaluationResponse(
            appliesToScope,
            requiresApprovalForAmount,
            shouldRun,
            wf.ApprovalThresholdAmount,
            message));
    }

    [HttpPut("{workflowId:guid}")]
    [ProducesResponseType(typeof(WorkflowDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid workflowId, [FromBody] UpdateWorkflowRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var wf = await _db.Workflows.FirstOrDefaultAsync(x => x.WorkflowId == workflowId);
        if (wf is null)
            return NotFound();

        var name = request.Name.Trim();
        var dup = await _db.Workflows.AsNoTracking()
            .AnyAsync(x => x.WorkflowId != workflowId && x.Name == name);
        if (dup)
            return Conflict(new { message = "Workflow name already exists." });

        var thresholdError = ValidateThreshold(request.ApprovalThresholdAmount);
        if (thresholdError is not null)
            return BadRequest(new { message = thresholdError });

        var scopeError = await ValidateScopeAsync(
            tenantId,
            request.ApplyToAllBusinessUnits,
            request.BusinessUnitIds,
            request.ApplyToAllDepartments,
            request.DepartmentIds,
            request.ApplyToAllExpenseCategories,
            request.ExpenseCategoryIds);
        if (scopeError is not null)
            return BadRequest(new { message = scopeError });

        var (buIds, deptIds, catIds) = NormalizeScopeIds(
            request.ApplyToAllBusinessUnits, request.BusinessUnitIds,
            request.ApplyToAllDepartments, request.DepartmentIds,
            request.ApplyToAllExpenseCategories, request.ExpenseCategoryIds);

        if (request.Steps is not null)
        {
            var stepError = await ValidateStepInputsAsync(tenantId, request.Steps);
            if (stepError is not null)
                return BadRequest(new { message = stepError });
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            wf.Name = name;
            wf.Description = request.Description?.Trim();
            wf.ApplyToAllBusinessUnits = request.ApplyToAllBusinessUnits;
            wf.ApplyToAllDepartments = request.ApplyToAllDepartments;
            wf.ApplyToAllExpenseCategories = request.ApplyToAllExpenseCategories;
            wf.ApprovalThresholdAmount = request.ApprovalThresholdAmount;
            wf.IsActive = request.IsActive;
            wf.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceScopeRowsAsync(workflowId, wf.TenantId, buIds, deptIds, catIds);

            if (request.Steps is not null)
                await ReplaceWorkflowStepsCoreAsync(workflowId, request.Steps);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var reloaded = await LoadWorkflowForDetailAsync(workflowId);
        return Ok(await ToDetailResponseAsync(reloaded!));
    }

    [HttpDelete("{workflowId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid workflowId)
    {
        var wf = await _db.Workflows.FirstOrDefaultAsync(x => x.WorkflowId == workflowId);
        if (wf is null)
            return NotFound();

        wf.IsActive = false;
        wf.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Replace the full approval chain (reorder, add/remove approvers).</summary>
    [HttpPut("{workflowId:guid}/steps")]
    [ProducesResponseType(typeof(WorkflowDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceSteps(
        [FromRoute] Guid workflowId,
        [FromBody] ReplaceWorkflowStepsRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var wf = await _db.Workflows.FirstOrDefaultAsync(x => x.WorkflowId == workflowId);
        if (wf is null)
            return NotFound();

        var stepError = await ValidateStepInputsAsync(tenantId, request.Steps);
        if (stepError is not null)
            return BadRequest(new { message = stepError });

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            await ReplaceWorkflowStepsCoreAsync(workflowId, request.Steps);

            wf.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var reloaded = await LoadWorkflowForDetailAsync(workflowId);
        return Ok(await ToDetailResponseAsync(reloaded!));
    }

    /// <summary>Append one approver as the next step in the chain.</summary>
    [HttpPost("{workflowId:guid}/steps")]
    [ProducesResponseType(typeof(WorkflowDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AppendStep(
        [FromRoute] Guid workflowId,
        [FromBody] AppendWorkflowStepRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var wf = await _db.Workflows.FirstOrDefaultAsync(x => x.WorkflowId == workflowId);
        if (wf is null)
            return NotFound();

        if (!await IsActiveTenantMemberAsync(tenantId, request.ApproverUserId))
            return BadRequest(new { message = "Approver is not an active member of this tenant." });

        var maxSeq = await _db.WorkflowSteps.Where(x => x.WorkflowId == workflowId).MaxAsync(x => (int?)x.Sequence) ?? 0;

        _db.WorkflowSteps.Add(new WorkflowStep
        {
            WorkflowStepId = Guid.NewGuid(),
            WorkflowId = workflowId,
            Sequence = maxSeq + 1,
            ApproverUserId = request.ApproverUserId
        });

        wf.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var reloaded = await LoadWorkflowForDetailAsync(workflowId);
        return Ok(await ToDetailResponseAsync(reloaded!));
    }

    [HttpPut("{workflowId:guid}/steps/{stepId:guid}")]
    [ProducesResponseType(typeof(WorkflowDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStep(
        [FromRoute] Guid workflowId,
        [FromRoute] Guid stepId,
        [FromBody] UpdateWorkflowStepRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var tenantId = _tenant.TenantId.Value;

        var step = await _db.WorkflowSteps
            .Include(x => x.Workflow)
            .FirstOrDefaultAsync(x => x.WorkflowStepId == stepId && x.WorkflowId == workflowId);

        if (step is null)
            return NotFound();

        if (!await IsActiveTenantMemberAsync(tenantId, request.ApproverUserId))
            return BadRequest(new { message = "Approver is not an active member of this tenant." });

        step.ApproverUserId = request.ApproverUserId;
        step.Workflow.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var reloaded = await LoadWorkflowForDetailAsync(workflowId);
        return Ok(await ToDetailResponseAsync(reloaded!));
    }

    [HttpDelete("{workflowId:guid}/steps/{stepId:guid}")]
    [ProducesResponseType(typeof(WorkflowDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStep([FromRoute] Guid workflowId, [FromRoute] Guid stepId)
    {
        var step = await _db.WorkflowSteps
            .Include(x => x.Workflow)
            .FirstOrDefaultAsync(x => x.WorkflowStepId == stepId && x.WorkflowId == workflowId);

        if (step is null)
            return NotFound();

        _db.WorkflowSteps.Remove(step);
        step.Workflow.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await RenumberStepsAsync(workflowId);

        var reloaded = await LoadWorkflowForDetailAsync(workflowId);
        return Ok(await ToDetailResponseAsync(reloaded!));
    }

    private async Task RenumberStepsAsync(Guid workflowId)
    {
        var steps = await _db.WorkflowSteps
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.Sequence)
            .ToListAsync();

        for (var i = 0; i < steps.Count; i++)
            steps[i].Sequence = i + 1;

        await _db.SaveChangesAsync();
    }

    private async Task ReplaceWorkflowStepsCoreAsync(Guid workflowId, IReadOnlyList<WorkflowStepInput> steps)
    {
        await _db.WorkflowSteps.Where(x => x.WorkflowId == workflowId).ExecuteDeleteAsync();

        foreach (var s in steps.OrderBy(x => x.Sequence))
        {
            _db.WorkflowSteps.Add(new WorkflowStep
            {
                WorkflowStepId = Guid.NewGuid(),
                WorkflowId = workflowId,
                Sequence = s.Sequence,
                ApproverUserId = s.ApproverUserId
            });
        }
    }

    private async Task<string?> ValidateStepInputsAsync(Guid tenantId, IReadOnlyList<WorkflowStepInput> steps)
    {
        if (steps.Count == 0)
            return null;

        var sequences = steps.Select(s => s.Sequence).ToList();
        if (sequences.Any(s => s < 1))
            return "Each step sequence must be at least 1.";

        if (sequences.Count != sequences.Distinct().Count())
            return "Step sequences must be unique within the workflow.";

        foreach (var s in steps)
        {
            if (!await IsActiveTenantMemberAsync(tenantId, s.ApproverUserId))
                return $"User {s.ApproverUserId} is not an active member of this tenant.";
        }

        return null;
    }

    private static (List<Guid> Bu, List<Guid> Dept, List<Guid> Cat) NormalizeScopeIds(
        bool applyAllBu, List<Guid> buIds,
        bool applyAllDept, List<Guid> deptIds,
        bool applyAllCat, List<Guid> catIds) =>
        (
            applyAllBu ? [] : buIds.Distinct().ToList(),
            applyAllDept ? [] : deptIds.Distinct().ToList(),
            applyAllCat ? [] : catIds.Distinct().ToList()
        );

    private async Task<string?> ValidateScopeAsync(
        Guid tenantId,
        bool applyAllBu, List<Guid> buIds,
        bool applyAllDept, List<Guid> deptIds,
        bool applyAllCat, List<Guid> catIds)
    {
        if (!applyAllBu && buIds.Count == 0)
            return "When not applying to all business units, provide at least one business unit id.";
        if (!applyAllDept && deptIds.Count == 0)
            return "When not applying to all departments, provide at least one department id.";
        if (!applyAllCat && catIds.Count == 0)
            return "When not applying to all expense categories, provide at least one expense category id.";

        var (buD, deptD, catD) = NormalizeScopeIds(applyAllBu, buIds, applyAllDept, deptIds, applyAllCat, catIds);

        if (buD.Count > 0)
        {
            var ok = await _db.BusinessUnits.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive && buD.Contains(x.BusinessUnitId))
                .CountAsync();
            if (ok != buD.Count)
                return "One or more business units are invalid or inactive.";
        }

        if (deptD.Count > 0)
        {
            var ok = await _db.Departments.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive && deptD.Contains(x.DepartmentId))
                .CountAsync();
            if (ok != deptD.Count)
                return "One or more departments are invalid or inactive.";
        }

        if (catD.Count > 0)
        {
            var ok = await _db.ExpenseCategories.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive && catD.Contains(x.ExpenseCategoryId))
                .CountAsync();
            if (ok != catD.Count)
                return "One or more expense categories are invalid or inactive.";
        }

        return null;
    }

    private static void AddScopeRows(Workflow wf, List<Guid> buIds, List<Guid> deptIds, List<Guid> catIds)
    {
        foreach (var id in buIds)
        {
            wf.BusinessUnitScopes.Add(new WorkflowBusinessUnitScope
            {
                WorkflowBusinessUnitScopeId = Guid.NewGuid(),
                TenantId = wf.TenantId,
                WorkflowId = wf.WorkflowId,
                BusinessUnitId = id
            });
        }

        foreach (var id in deptIds)
        {
            wf.DepartmentScopes.Add(new WorkflowDepartmentScope
            {
                WorkflowDepartmentScopeId = Guid.NewGuid(),
                TenantId = wf.TenantId,
                WorkflowId = wf.WorkflowId,
                DepartmentId = id
            });
        }

        foreach (var id in catIds)
        {
            wf.ExpenseCategoryScopes.Add(new WorkflowExpenseCategoryScope
            {
                WorkflowExpenseCategoryScopeId = Guid.NewGuid(),
                TenantId = wf.TenantId,
                WorkflowId = wf.WorkflowId,
                ExpenseCategoryId = id
            });
        }
    }

    private async Task ReplaceScopeRowsAsync(Guid workflowId, Guid tenantId, List<Guid> buIds, List<Guid> deptIds, List<Guid> catIds)
    {
        await _db.WorkflowBusinessUnitScopes.Where(x => x.WorkflowId == workflowId).ExecuteDeleteAsync();
        await _db.WorkflowDepartmentScopes.Where(x => x.WorkflowId == workflowId).ExecuteDeleteAsync();
        await _db.WorkflowExpenseCategoryScopes.Where(x => x.WorkflowId == workflowId).ExecuteDeleteAsync();

        foreach (var id in buIds)
        {
            _db.WorkflowBusinessUnitScopes.Add(new WorkflowBusinessUnitScope
            {
                WorkflowBusinessUnitScopeId = Guid.NewGuid(),
                TenantId = tenantId,
                WorkflowId = workflowId,
                BusinessUnitId = id
            });
        }

        foreach (var id in deptIds)
        {
            _db.WorkflowDepartmentScopes.Add(new WorkflowDepartmentScope
            {
                WorkflowDepartmentScopeId = Guid.NewGuid(),
                TenantId = tenantId,
                WorkflowId = workflowId,
                DepartmentId = id
            });
        }

        foreach (var id in catIds)
        {
            _db.WorkflowExpenseCategoryScopes.Add(new WorkflowExpenseCategoryScope
            {
                WorkflowExpenseCategoryScopeId = Guid.NewGuid(),
                TenantId = tenantId,
                WorkflowId = workflowId,
                ExpenseCategoryId = id
            });
        }
    }

    private Task<bool> IsActiveTenantMemberAsync(Guid tenantId, Guid userId) =>
        _db.TenantUsers.AsNoTracking()
            .AnyAsync(tu => tu.TenantId == tenantId && tu.UserId == userId && tu.IsActive);

    private Task<Workflow?> LoadWorkflowForDetailAsync(Guid workflowId) =>
        _db.Workflows.AsNoTracking()
            .Include(x => x.Steps)
            .Include(x => x.BusinessUnitScopes).ThenInclude(x => x.BusinessUnit)
            .Include(x => x.DepartmentScopes).ThenInclude(x => x.Department)
            .Include(x => x.ExpenseCategoryScopes).ThenInclude(x => x.ExpenseCategory)
            .FirstOrDefaultAsync(x => x.WorkflowId == workflowId);

    private async Task<WorkflowDetailResponse> ToDetailResponseAsync(Guid workflowId)
    {
        var wf = await LoadWorkflowForDetailAsync(workflowId);
        if (wf is null)
            throw new InvalidOperationException("Workflow not found.");
        return await ToDetailResponseAsync(wf);
    }

    private async Task<WorkflowDetailResponse> ToDetailResponseAsync(Workflow wf)
    {
        var ordered = wf.Steps.OrderBy(s => s.Sequence).ToList();
        var userIds = ordered.Select(s => s.ApproverUserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var stepDtos = ordered.Select(s =>
        {
            users.TryGetValue(s.ApproverUserId, out var u);
            return new WorkflowStepResponse(
                s.WorkflowStepId,
                s.Sequence,
                s.ApproverUserId,
                u?.Email,
                u?.FullName);
        }).ToList();

        var scope = BuildScopeResponse(wf);

        return new WorkflowDetailResponse(
            wf.WorkflowId,
            wf.Name,
            wf.Description,
            scope,
            wf.ApprovalThresholdAmount,
            wf.IsActive,
            stepDtos,
            wf.CreatedAtUtc,
            wf.UpdatedAtUtc);
    }

    private static WorkflowScopeResponse BuildScopeResponse(Workflow wf)
    {
        var bus = wf.ApplyToAllBusinessUnits
            ? (IReadOnlyList<WorkflowScopeItemResponse>)Array.Empty<WorkflowScopeItemResponse>()
            : wf.BusinessUnitScopes
                .OrderBy(x => x.BusinessUnit.Name)
                .Select(x => new WorkflowScopeItemResponse(x.BusinessUnitId, x.BusinessUnit.Name))
                .ToList();

        var deps = wf.ApplyToAllDepartments
            ? (IReadOnlyList<WorkflowScopeItemResponse>)Array.Empty<WorkflowScopeItemResponse>()
            : wf.DepartmentScopes
                .OrderBy(x => x.Department.Name)
                .Select(x => new WorkflowScopeItemResponse(x.DepartmentId, x.Department.Name))
                .ToList();

        var cats = wf.ApplyToAllExpenseCategories
            ? (IReadOnlyList<WorkflowScopeItemResponse>)Array.Empty<WorkflowScopeItemResponse>()
            : wf.ExpenseCategoryScopes
                .OrderBy(x => x.ExpenseCategory.Name)
                .Select(x => new WorkflowScopeItemResponse(x.ExpenseCategoryId, x.ExpenseCategory.Name))
                .ToList();

        return new WorkflowScopeResponse(
            wf.ApplyToAllBusinessUnits,
            wf.ApplyToAllDepartments,
            wf.ApplyToAllExpenseCategories,
            bus,
            deps,
            cats);
    }

    private static string? ValidateThreshold(decimal? amount)
    {
        if (!amount.HasValue)
            return null;
        if (amount.Value < 0)
            return "Approval threshold amount cannot be negative.";
        return null;
    }
}
