using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Contracts.ExpenseRequests;
using MultiTenant.Api.Domain.Entities;
using MultiTenant.Api.Identity;
using MultiTenant.Api.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;

namespace MultiTenant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/expense-requests")]
public sealed class ExpenseRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public ExpenseRequestsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private Guid? CurrentUserId()
    {
        var s = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ExpenseRequestDashboardCounts), StatusCodes.Status200OK)]
    public async Task<IActionResult> DashboardCounts()
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var q = _db.ExpenseRequests.AsNoTracking().Where(x => x.IsActive);

        var counts = new ExpenseRequestDashboardCounts(
            await q.CountAsync(),
            await q.CountAsync(x => x.Status == ExpenseRequestStatus.Draft),
            await q.CountAsync(x => x.Status == ExpenseRequestStatus.PendingApproval),
            await q.CountAsync(x => x.Status == ExpenseRequestStatus.Approved),
            await q.CountAsync(x => x.Status == ExpenseRequestStatus.Rejected),
            await q.CountAsync(x => x.Status == ExpenseRequestStatus.Completed));

        return Ok(counts);
    }

    /// <summary>KPI cards, year spend, counts, and quick-link paths for the dashboard UI.</summary>
    [HttpGet("dashboard/overview")]
    [ProducesResponseType(typeof(ExpenseRequestDashboardOverview), StatusCodes.Status200OK)]
    public async Task<IActionResult> DashboardOverview([FromQuery] bool forCurrentUser = true)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var scope = ScopedExpenseRequests(forCurrentUser);
        var overview = await BuildDashboardOverviewAsync(scope, forCurrentUser);
        return Ok(overview);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ExpenseRequestListPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] ExpenseRequestStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] bool mine = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var summaryScope = ScopedExpenseRequests(mine);
        var counts = await BuildDashboardCountsAsync(summaryScope);

        var q = summaryScope;

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.RequestNumber.Contains(s) || x.Title.Contains(s));
        }

        var totalCount = await q.CountAsync();

        var rows = await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.ExpenseCategory)
            .Include(x => x.Approvals)
            .ToListAsync();

        var userIds = new HashSet<Guid>();
        foreach (var e in rows)
        {
            userIds.Add(e.SubmittedByUserId);
            if (e.Approvals is not null)
            {
                foreach (var a in e.Approvals)
                    userIds.Add(a.ApproverUserId);
            }
        }

        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var items = rows.Select(e => MapToListItem(e, users)).ToList();

        return Ok(new ExpenseRequestListPageResponse(counts, items, totalCount, page, pageSize));
    }

    [HttpGet("{expenseRequestId:guid}")]
    [ProducesResponseType(typeof(ExpenseRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid expenseRequestId)
    {
        var dto = await ToDetailAsync(expenseRequestId);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("by-number/{requestNumber}")]
    [ProducesResponseType(typeof(ExpenseRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber([FromRoute] string requestNumber)
    {
        var id = await _db.ExpenseRequests.AsNoTracking()
            .Where(x => x.RequestNumber == requestNumber)
            .Select(x => (Guid?)x.ExpenseRequestId)
            .FirstOrDefaultAsync();

        if (!id.HasValue)
            return NotFound();

        var dto = await ToDetailAsync(id.Value);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseRequestDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequestRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var uid = CurrentUserId();
        if (!uid.HasValue)
            return Unauthorized("Invalid user.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required." });

        var lines = request.Lines ?? new List<ExpenseRequestLineInput>();

        var tenantId = _tenant.TenantId.Value;
        var fundingBudgetErr = ValidateFundingAndBudget(request.FundingType, request.BudgetId);
        if (fundingBudgetErr is not null)
            return BadRequest(new { message = fundingBudgetErr });
        var effectiveBudgetId = request.FundingType == ExpenseRequestFundingType.SpecialApproval ? null : request.BudgetId;
        var err = await ValidateHeaderRefsAsync(tenantId, request.VendorId, request.ExpenseCategoryId, request.DepartmentId, request.BusinessUnitId, effectiveBudgetId);
        if (err is not null)
            return BadRequest(new { message = err });
        var projectErr = ValidateProjectId(request.ProjectId);
        if (projectErr is not null)
            return BadRequest(new { message = projectErr });

        var lineErr = await ValidateLineInputsAsync(tenantId, lines);
        if (lineErr is not null)
            return BadRequest(new { message = lineErr });

        var total = lines.Sum(x => x.Amount);
        if (total <= 0)
            return BadRequest(new { message = "At least one line with a positive amount is required." });

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var requestNumber = await AllocateRequestNumberAsync(tenantId);
            var entity = new ExpenseRequest
            {
                ExpenseRequestId = Guid.NewGuid(),
                TenantId = tenantId,
                RequestNumber = requestNumber,
                SubmittedByUserId = uid.Value,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                ExpenseType = request.ExpenseType,
                ProjectId = NormalizeProjectId(request.ProjectId),
                FundingType = request.FundingType,
                Status = ExpenseRequestStatus.Draft,
                TotalAmount = total,
                CurrencyCode = NormalizeCurrency(request.CurrencyCode) ?? "USD",
                VendorId = request.VendorId,
                ExpenseCategoryId = request.ExpenseCategoryId,
                DepartmentId = request.DepartmentId,
                BusinessUnitId = request.BusinessUnitId,
                BudgetId = effectiveBudgetId,
                CreatedAtUtc = DateTime.UtcNow
            };

            AddLines(entity, lines);
            _db.ExpenseRequests.Add(entity);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { expenseRequestId = entity.ExpenseRequestId }, await ToDetailAsync(entity.ExpenseRequestId));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{expenseRequestId:guid}")]
    [ProducesResponseType(typeof(ExpenseRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid expenseRequestId, [FromBody] UpdateExpenseRequestRequest request)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        var uid = CurrentUserId();
        if (!uid.HasValue)
            return Unauthorized("Invalid user.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required." });

        var lines = request.Lines ?? new List<ExpenseRequestLineInput>();

        var tenantId = _tenant.TenantId.Value;
        var entity = await _db.ExpenseRequests.FirstOrDefaultAsync(x => x.ExpenseRequestId == expenseRequestId);
        if (entity is null)
            return NotFound();

        if (entity.SubmittedByUserId != uid.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only the submitter can edit this request." });
        if (entity.Status != ExpenseRequestStatus.Draft)
            return BadRequest(new { message = "Only draft requests can be updated." });

        var fundingBudgetErr = ValidateFundingAndBudget(request.FundingType, request.BudgetId);
        if (fundingBudgetErr is not null)
            return BadRequest(new { message = fundingBudgetErr });
        var effectiveBudgetId = request.FundingType == ExpenseRequestFundingType.SpecialApproval ? null : request.BudgetId;
        var err = await ValidateHeaderRefsAsync(tenantId, request.VendorId, request.ExpenseCategoryId, request.DepartmentId, request.BusinessUnitId, effectiveBudgetId);
        if (err is not null)
            return BadRequest(new { message = err });
        var projectErr = ValidateProjectId(request.ProjectId);
        if (projectErr is not null)
            return BadRequest(new { message = projectErr });

        var lineErr = await ValidateLineInputsAsync(tenantId, lines);
        if (lineErr is not null)
            return BadRequest(new { message = lineErr });

        var total = lines.Sum(x => x.Amount);
        if (total <= 0)
            return BadRequest(new { message = "At least one line with a positive amount is required." });

        entity.Title = request.Title.Trim();
        entity.Description = request.Description?.Trim();
        entity.ExpenseType = request.ExpenseType;
        entity.ProjectId = NormalizeProjectId(request.ProjectId);
        entity.FundingType = request.FundingType;
        entity.TotalAmount = total;
        entity.CurrencyCode = NormalizeCurrency(request.CurrencyCode) ?? entity.CurrencyCode;
        entity.VendorId = request.VendorId;
        entity.ExpenseCategoryId = request.ExpenseCategoryId;
        entity.DepartmentId = request.DepartmentId;
        entity.BusinessUnitId = request.BusinessUnitId;
        entity.BudgetId = effectiveBudgetId;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.ExpenseRequestLines.Where(x => x.ExpenseRequestId == expenseRequestId).ExecuteDeleteAsync();
        AddLines(entity, lines);

        await _db.SaveChangesAsync();
        return Ok(await ToDetailAsync(expenseRequestId));
    }

    [HttpDelete("{expenseRequestId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid expenseRequestId)
    {
        var uid = CurrentUserId();
        if (!uid.HasValue)
            return Unauthorized("Invalid user.");

        var entity = await _db.ExpenseRequests.FirstOrDefaultAsync(x => x.ExpenseRequestId == expenseRequestId);
        if (entity is null)
            return NotFound();
        if (entity.SubmittedByUserId != uid.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only the submitter can delete this request." });
        if (entity.Status != ExpenseRequestStatus.Draft)
            return BadRequest(new { message = "Only draft requests can be deleted." });

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{expenseRequestId:guid}/submit")]
    [ProducesResponseType(typeof(ExpenseRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromRoute] Guid expenseRequestId, [FromBody] SubmitExpenseRequestRequest body)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized("Tenant not resolved.");

        var uid = CurrentUserId();
        if (!uid.HasValue)
            return Unauthorized("Invalid user.");

        var tenantId = _tenant.TenantId.Value;

        if (body is null)
            return BadRequest(new { message = "Request body is required (workflowId)." });

        if (body.WorkflowId == Guid.Empty)
            return BadRequest(new { message = "workflowId is required." });

        var entity = await _db.ExpenseRequests
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.ExpenseRequestId == expenseRequestId);
        if (entity is null)
            return NotFound();
        if (entity.SubmittedByUserId != uid.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only the submitter can submit." });
        if (entity.Status != ExpenseRequestStatus.Draft)
            return BadRequest(new { message = "Request is not in draft status." });
        if (entity.Lines is null || entity.Lines.Count == 0)
            return BadRequest(new { message = "Add at least one line before submitting." });

        var wf = await LoadWorkflowWithScopesAndStepsAsync(body.WorkflowId);
        if (wf is null || !wf.IsActive)
            return BadRequest(new { message = "Workflow not found or inactive." });

        if (!wf.AppliesToScope(entity.BusinessUnitId, entity.DepartmentId, entity.ExpenseCategoryId))
            return BadRequest(new { message = "This workflow does not apply to the department / business unit / category on this request." });

        var runChain = wf.ShouldRunApprovalChain(entity.TotalAmount, entity.BusinessUnitId, entity.DepartmentId, entity.ExpenseCategoryId);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            entity.WorkflowId = wf.WorkflowId;
            entity.SubmittedAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            var steps = wf.Steps ?? new List<WorkflowStep>();
            if (!runChain || steps.Count == 0)
            {
                entity.Status = ExpenseRequestStatus.Approved;
                entity.ApprovedAtUtc = DateTime.UtcNow;
                entity.CurrentApprovalStepSequence = null;
            }
            else
            {
                entity.Status = ExpenseRequestStatus.PendingApproval;
                entity.CurrentApprovalStepSequence = steps.OrderBy(s => s.Sequence).First().Sequence;

                foreach (var step in steps.OrderBy(s => s.Sequence))
                {
                    entity.Approvals.Add(new ExpenseRequestApproval
                    {
                        ExpenseRequestApprovalId = Guid.NewGuid(),
                        TenantId = tenantId,
                        ExpenseRequestId = entity.ExpenseRequestId,
                        StepSequence = step.Sequence,
                        ApproverUserId = step.ApproverUserId,
                        StepStatus = ExpenseRequestApprovalStatus.Pending
                    });
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return Ok(await ToDetailAsync(expenseRequestId));
    }

    [HttpPost("{expenseRequestId:guid}/approve")]
    [ProducesResponseType(typeof(ExpenseRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Approve([FromRoute] Guid expenseRequestId, [FromBody] ApproveExpenseRequestRequest? body)
    {
        var uid = CurrentUserId();
        if (!uid.HasValue)
            return Unauthorized("Invalid user.");

        var entity = await _db.ExpenseRequests
            .Include(x => x.Approvals)
            .FirstOrDefaultAsync(x => x.ExpenseRequestId == expenseRequestId);
        if (entity is null)
            return NotFound();
        if (entity.Status != ExpenseRequestStatus.PendingApproval)
            return BadRequest(new { message = "Request is not pending approval." });

        var currentSeq = entity.CurrentApprovalStepSequence;
        if (!currentSeq.HasValue)
            return BadRequest(new { message = "No pending approval step." });

        var row = entity.Approvals.FirstOrDefault(x =>
            x.StepSequence == currentSeq.Value &&
            x.ApproverUserId == uid.Value &&
            x.StepStatus == ExpenseRequestApprovalStatus.Pending);
        if (row is null)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not the approver for the current step." });

        row.StepStatus = ExpenseRequestApprovalStatus.Approved;
        row.ActionAtUtc = DateTime.UtcNow;
        row.Comment = body?.Comment?.Trim();

        var ordered = entity.Approvals.OrderBy(x => x.StepSequence).ToList();
        var nextPending = ordered.FirstOrDefault(x => x.StepSequence > currentSeq.Value && x.StepStatus == ExpenseRequestApprovalStatus.Pending);
        if (nextPending != null)
            entity.CurrentApprovalStepSequence = nextPending.StepSequence;
        else
        {
            entity.CurrentApprovalStepSequence = null;
            entity.Status = ExpenseRequestStatus.Approved;
            entity.ApprovedAtUtc = DateTime.UtcNow;
        }

        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(await ToDetailAsync(expenseRequestId));
    }

    [HttpPost("{expenseRequestId:guid}/reject")]
    [ProducesResponseType(typeof(ExpenseRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject([FromRoute] Guid expenseRequestId, [FromBody] RejectExpenseRequestRequest body)
    {
        var uid = CurrentUserId();
        if (!uid.HasValue)
            return Unauthorized("Invalid user.");

        if (string.IsNullOrWhiteSpace(body.Comment))
            return BadRequest(new { message = "Comment is required when rejecting." });

        var entity = await _db.ExpenseRequests
            .Include(x => x.Approvals)
            .FirstOrDefaultAsync(x => x.ExpenseRequestId == expenseRequestId);
        if (entity is null)
            return NotFound();
        if (entity.Status != ExpenseRequestStatus.PendingApproval)
            return BadRequest(new { message = "Request is not pending approval." });

        var currentSeq = entity.CurrentApprovalStepSequence;
        if (!currentSeq.HasValue)
            return BadRequest(new { message = "No pending approval step." });

        var row = entity.Approvals.FirstOrDefault(x =>
            x.StepSequence == currentSeq.Value &&
            x.ApproverUserId == uid.Value &&
            x.StepStatus == ExpenseRequestApprovalStatus.Pending);
        if (row is null)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not the approver for the current step." });

        row.StepStatus = ExpenseRequestApprovalStatus.Rejected;
        row.ActionAtUtc = DateTime.UtcNow;
        row.Comment = body.Comment.Trim();

        foreach (var a in entity.Approvals.Where(x => x.StepStatus == ExpenseRequestApprovalStatus.Pending && x.ExpenseRequestApprovalId != row.ExpenseRequestApprovalId))
            a.StepStatus = ExpenseRequestApprovalStatus.Skipped;

        entity.Status = ExpenseRequestStatus.Rejected;
        entity.RejectedAtUtc = DateTime.UtcNow;
        entity.CurrentApprovalStepSequence = null;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(await ToDetailAsync(expenseRequestId));
    }

    [HttpPost("{expenseRequestId:guid}/complete")]
    [ProducesResponseType(typeof(ExpenseRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete([FromRoute] Guid expenseRequestId)
    {
        var uid = CurrentUserId();
        if (!uid.HasValue)
            return Unauthorized("Invalid user.");

        var entity = await _db.ExpenseRequests.FirstOrDefaultAsync(x => x.ExpenseRequestId == expenseRequestId);
        if (entity is null)
            return NotFound();
        if (entity.Status != ExpenseRequestStatus.Approved)
            return BadRequest(new { message = "Only approved requests can be marked completed." });
        if (entity.SubmittedByUserId != uid.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only the submitter can mark completed." });

        entity.Status = ExpenseRequestStatus.Completed;
        entity.CompletedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(await ToDetailAsync(expenseRequestId));
    }

    private IQueryable<ExpenseRequest> ScopedExpenseRequests(bool restrictToSubmitter)
    {
        var q = _db.ExpenseRequests.AsNoTracking().Where(x => x.IsActive);
        if (!restrictToSubmitter)
            return q;

        var uid = CurrentUserId();
        return uid.HasValue ? q.Where(x => x.SubmittedByUserId == uid.Value) : q.Where(x => false);
    }

    private static async Task<ExpenseRequestDashboardCounts> BuildDashboardCountsAsync(IQueryable<ExpenseRequest> scope) =>
        new(
            await scope.CountAsync(),
            await scope.CountAsync(x => x.Status == ExpenseRequestStatus.Draft),
            await scope.CountAsync(x => x.Status == ExpenseRequestStatus.PendingApproval),
            await scope.CountAsync(x => x.Status == ExpenseRequestStatus.Approved),
            await scope.CountAsync(x => x.Status == ExpenseRequestStatus.Rejected),
            await scope.CountAsync(x => x.Status == ExpenseRequestStatus.Completed));

    private async Task<ExpenseRequestDashboardOverview> BuildDashboardOverviewAsync(IQueryable<ExpenseRequest> scope, bool forCurrentUser)
    {
        var counts = await BuildDashboardCountsAsync(scope);
        var (curStart, now, prevStart, curStartExclusive) = PeriodWindows30Day();

        var pendingCount = await scope.CountAsync(x => x.Status == ExpenseRequestStatus.PendingApproval);
        var pendingCur = await scope.CountAsync(x =>
            x.Status == ExpenseRequestStatus.PendingApproval &&
            x.SubmittedAtUtc.HasValue &&
            x.SubmittedAtUtc >= curStart &&
            x.SubmittedAtUtc <= now);
        var pendingPrev = await scope.CountAsync(x =>
            x.Status == ExpenseRequestStatus.PendingApproval &&
            x.SubmittedAtUtc.HasValue &&
            x.SubmittedAtUtc >= prevStart &&
            x.SubmittedAtUtc < curStartExclusive);
        var pending = new ExpenseRequestDashboardKpi(pendingCount, TrendInt(pendingPrev, pendingCur));

        var approvedCount = await scope.CountAsync(x => x.Status == ExpenseRequestStatus.Approved);
        var apprCur = await scope.CountAsync(x =>
            x.Status == ExpenseRequestStatus.Approved &&
            x.ApprovedAtUtc.HasValue &&
            x.ApprovedAtUtc >= curStart &&
            x.ApprovedAtUtc <= now);
        var apprPrev = await scope.CountAsync(x =>
            x.Status == ExpenseRequestStatus.Approved &&
            x.ApprovedAtUtc.HasValue &&
            x.ApprovedAtUtc >= prevStart &&
            x.ApprovedAtUtc < curStartExclusive);
        var approved = new ExpenseRequestDashboardKpi(approvedCount, TrendInt(apprPrev, apprCur));

        var completedCount = await scope.CountAsync(x => x.Status == ExpenseRequestStatus.Completed);
        var compCur = await scope.CountAsync(x =>
            x.Status == ExpenseRequestStatus.Completed &&
            x.CompletedAtUtc.HasValue &&
            x.CompletedAtUtc >= curStart &&
            x.CompletedAtUtc <= now);
        var compPrev = await scope.CountAsync(x =>
            x.Status == ExpenseRequestStatus.Completed &&
            x.CompletedAtUtc.HasValue &&
            x.CompletedAtUtc >= prevStart &&
            x.CompletedAtUtc < curStartExclusive);
        var completed = new ExpenseRequestDashboardKpi(completedCount, TrendInt(compPrev, compCur));

        var year = DateTime.UtcNow.Year;
        var spendScope = scope.Where(x =>
            (x.Status == ExpenseRequestStatus.Completed && x.CompletedAtUtc.HasValue && x.CompletedAtUtc.Value.Year == year) ||
            (x.Status == ExpenseRequestStatus.Approved && x.ApprovedAtUtc.HasValue && x.ApprovedAtUtc.Value.Year == year));
        var amount = await spendScope.SumAsync(x => x.TotalAmount);
        var currency = await spendScope.Select(x => x.CurrencyCode).FirstOrDefaultAsync() ?? "USD";

        var spendCur = await SumSpendApprovedCompletedInclusive(scope, curStart, now);
        var spendPrev = await SumSpendApprovedCompletedExclusiveEnd(scope, prevStart, curStartExclusive);
        var spend = new ExpenseRequestDashboardSpend(amount, currency, TrendDecimal(spendPrev, spendCur));

        return new ExpenseRequestDashboardOverview(counts, pending, approved, completed, spend, BuildQuickLinks(forCurrentUser));
    }

    private static (DateTime curStart, DateTime now, DateTime prevStart, DateTime curStartExclusive) PeriodWindows30Day()
    {
        var now = DateTime.UtcNow;
        var curStart = now.AddDays(-30);
        var prevStart = now.AddDays(-60);
        return (curStart, now, prevStart, curStart);
    }

    private static decimal? TrendInt(int previousPeriodCount, int currentPeriodCount)
    {
        if (previousPeriodCount == 0)
            return currentPeriodCount == 0 ? 0 : null;

        return Math.Round((currentPeriodCount - previousPeriodCount) / (decimal)previousPeriodCount * 100m, 1, MidpointRounding.AwayFromZero);
    }

    private static decimal? TrendDecimal(decimal previousPeriodSum, decimal currentPeriodSum)
    {
        if (previousPeriodSum == 0)
            return currentPeriodSum == 0 ? 0 : null;

        return Math.Round((currentPeriodSum - previousPeriodSum) / previousPeriodSum * 100m, 1, MidpointRounding.AwayFromZero);
    }

    private static async Task<decimal> SumSpendApprovedCompletedInclusive(IQueryable<ExpenseRequest> q, DateTime startInclusive, DateTime endInclusive)
    {
        return await q.Where(x =>
                (x.Status == ExpenseRequestStatus.Completed &&
                 x.CompletedAtUtc.HasValue &&
                 x.CompletedAtUtc.Value >= startInclusive &&
                 x.CompletedAtUtc.Value <= endInclusive) ||
                (x.Status == ExpenseRequestStatus.Approved &&
                 x.ApprovedAtUtc.HasValue &&
                 x.ApprovedAtUtc.Value >= startInclusive &&
                 x.ApprovedAtUtc.Value <= endInclusive))
            .SumAsync(x => x.TotalAmount);
    }

    private static async Task<decimal> SumSpendApprovedCompletedExclusiveEnd(IQueryable<ExpenseRequest> q, DateTime startInclusive, DateTime endExclusive)
    {
        return await q.Where(x =>
                (x.Status == ExpenseRequestStatus.Completed &&
                 x.CompletedAtUtc.HasValue &&
                 x.CompletedAtUtc.Value >= startInclusive &&
                 x.CompletedAtUtc.Value < endExclusive) ||
                (x.Status == ExpenseRequestStatus.Approved &&
                 x.ApprovedAtUtc.HasValue &&
                 x.ApprovedAtUtc.Value >= startInclusive &&
                 x.ApprovedAtUtc.Value < endExclusive))
            .SumAsync(x => x.TotalAmount);
    }

    private static ExpenseRequestDashboardQuickLinks BuildQuickLinks(bool forCurrentUser)
    {
        var mine = forCurrentUser ? "mine=true&" : string.Empty;
        return new ExpenseRequestDashboardQuickLinks(
            $"api/expense-requests?{mine}status=PendingApproval",
            $"api/expense-requests?{mine}status=Approved",
            $"api/expense-requests?{mine}status=Completed",
            string.IsNullOrEmpty(mine) ? "api/expense-requests" : "api/expense-requests?mine=true",
            "expense-requests/create");
    }

    private static ExpenseRequestListItemResponse MapToListItem(ExpenseRequest e, IReadOnlyDictionary<Guid, ApplicationUser> users)
    {
        users.TryGetValue(e.SubmittedByUserId, out var sub);

        Guid? approvedByUserId = null;
        Guid? pendingApproverUserId = null;

        var approvals = e.Approvals;

        if (e.Status == ExpenseRequestStatus.PendingApproval && e.CurrentApprovalStepSequence is int seq && approvals is not null)
        {
            var pend = approvals.FirstOrDefault(a =>
                a.StepSequence == seq && a.StepStatus == ExpenseRequestApprovalStatus.Pending);
            pendingApproverUserId = pend?.ApproverUserId;
        }

        if (approvals is not null &&
            (e.Status == ExpenseRequestStatus.Approved || e.Status == ExpenseRequestStatus.Completed))
        {
            var last = approvals
                .Where(a => a.StepStatus == ExpenseRequestApprovalStatus.Approved)
                .OrderByDescending(a => a.StepSequence)
                .FirstOrDefault();
            approvedByUserId = last?.ApproverUserId;
        }

        ApplicationUser? appr = null;
        if (approvedByUserId.HasValue)
            users.TryGetValue(approvedByUserId.Value, out appr);

        ApplicationUser? pendU = null;
        if (pendingApproverUserId.HasValue)
            users.TryGetValue(pendingApproverUserId.Value, out pendU);

        return new ExpenseRequestListItemResponse(
            e.ExpenseRequestId,
            e.RequestNumber,
            e.Title,
            e.ExpenseType,
            e.ProjectId,
            e.FundingType,
            e.Status,
            e.TotalAmount,
            e.CurrencyCode,
            e.SubmittedByUserId,
            sub?.Email,
            sub?.FullName,
            e.CreatedAtUtc,
            e.SubmittedAtUtc,
            e.ExpenseCategoryId,
            e.ExpenseCategory?.Name,
            approvedByUserId,
            appr?.Email,
            appr?.FullName,
            pendingApproverUserId,
            pendU?.Email,
            pendU?.FullName);
    }

    private async Task<string> AllocateRequestNumberAsync(Guid tenantId)
    {
        var year = DateTime.UtcNow.Year;
        var seq = await _db.ExpenseRequestSequences
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Year == year);

        if (seq is null)
        {
            seq = new ExpenseRequestSequence
            {
                ExpenseRequestSequenceId = Guid.NewGuid(),
                TenantId = tenantId,
                Year = year,
                LastNumber = 1
            };
            _db.ExpenseRequestSequences.Add(seq);
            await _db.SaveChangesAsync();
            return $"REQ-{year}-0001";
        }

        seq.LastNumber++;
        await _db.SaveChangesAsync();
        return $"REQ-{year}-{seq.LastNumber:D4}";
    }

    private async Task<Workflow?> LoadWorkflowWithScopesAndStepsAsync(Guid workflowId) =>
        await _db.Workflows.AsNoTracking()
            .Include(x => x.Steps)
            .Include(x => x.BusinessUnitScopes)
            .Include(x => x.DepartmentScopes)
            .Include(x => x.ExpenseCategoryScopes)
            .FirstOrDefaultAsync(x => x.WorkflowId == workflowId);

    private static void AddLines(ExpenseRequest entity, IReadOnlyList<ExpenseRequestLineInput> lines)
    {
        var order = 0;
        foreach (var l in lines)
        {
            order++;
            entity.Lines.Add(new ExpenseRequestLine
            {
                ExpenseRequestLineId = Guid.NewGuid(),
                TenantId = entity.TenantId,
                ExpenseRequestId = entity.ExpenseRequestId,
                SequenceOrder = order,
                Description = l.Description.Trim(),
                Amount = l.Amount,
                ExpenseCategoryId = l.ExpenseCategoryId,
                GlAccountId = l.GlAccountId,
                VendorId = entity.VendorId
            });
        }
    }

    private async Task<string?> ValidateHeaderRefsAsync(
        Guid tenantId,
        Guid? vendorId,
        Guid? expenseCategoryId,
        Guid? departmentId,
        Guid? businessUnitId,
        Guid? budgetId)
    {
        if (vendorId.HasValue && !await _db.Vendors.AsNoTracking().AnyAsync(x => x.VendorId == vendorId.Value && x.TenantId == tenantId && x.IsActive))
            return "Vendor is invalid or inactive.";
        if (expenseCategoryId.HasValue && !await _db.ExpenseCategories.AsNoTracking().AnyAsync(x => x.ExpenseCategoryId == expenseCategoryId.Value && x.TenantId == tenantId && x.IsActive))
            return "Expense category is invalid or inactive.";
        if (departmentId.HasValue && !await _db.Departments.AsNoTracking().AnyAsync(x => x.DepartmentId == departmentId.Value && x.TenantId == tenantId && x.IsActive))
            return "Department is invalid or inactive.";
        if (businessUnitId.HasValue)
        {
            var bu = await _db.BusinessUnits.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BusinessUnitId == businessUnitId.Value && x.TenantId == tenantId && x.IsActive);
            if (bu is null)
                return "Business unit is invalid or inactive.";
            if (departmentId.HasValue && bu.DepartmentId != departmentId.Value)
                return "Business unit must belong to the selected department.";
        }

        if (budgetId.HasValue)
        {
            var bud = await _db.Budgets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BudgetId == budgetId.Value && x.TenantId == tenantId && x.IsActive);
            if (bud is null)
                return "Budget is invalid or inactive.";
            if (businessUnitId.HasValue && bud.BusinessUnitId != businessUnitId.Value)
                return "Budget is tied to a different business unit than the request.";
        }

        return null;
    }

    private async Task<string?> ValidateLineInputsAsync(Guid tenantId, IReadOnlyList<ExpenseRequestLineInput>? lines)
    {
        if (lines is null || lines.Count == 0)
            return "At least one line is required.";

        foreach (var l in lines)
        {
            if (string.IsNullOrWhiteSpace(l.Description))
                return "Each line requires a description.";
            if (l.Amount <= 0)
                return "Each line amount must be greater than zero.";
            if (l.ExpenseCategoryId.HasValue && !await _db.ExpenseCategories.AsNoTracking().AnyAsync(x => x.ExpenseCategoryId == l.ExpenseCategoryId.Value && x.TenantId == tenantId && x.IsActive))
                return "A line references an invalid expense category.";
            if (l.GlAccountId.HasValue && !await _db.GlAccounts.AsNoTracking().AnyAsync(x => x.GlAccountId == l.GlAccountId.Value && x.TenantId == tenantId && x.IsActive))
                return "A line references an invalid GL account.";
        }

        return null;
    }

    private static string? NormalizeCurrency(string? c) =>
        string.IsNullOrWhiteSpace(c) ? null : (c.Trim().Length == 3 ? c.Trim().ToUpperInvariant() : null);

    private static string? NormalizeProjectId(string? projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return null;
        var t = projectId.Trim();
        return t.Length == 0 ? null : t;
    }

    private static string? ValidateProjectId(string? projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return null;
        if (projectId.Trim().Length > 200)
            return "Project ID must be at most 200 characters.";
        return null;
    }

    private static string? ValidateFundingAndBudget(ExpenseRequestFundingType fundingType, Guid? budgetId)
    {
        if (fundingType == ExpenseRequestFundingType.SpecialApproval && budgetId.HasValue)
            return "Budget cannot be set when special approval is selected.";
        return null;
    }

    private async Task<ExpenseRequestDetailResponse?> ToDetailAsync(Guid expenseRequestId)
    {
        var e = await _db.ExpenseRequests.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(x => x.LineExpenseCategory)
            .Include(x => x.Lines).ThenInclude(x => x.GlAccount)
            .Include(x => x.Lines).ThenInclude(x => x.LineVendor)
            .Include(x => x.Approvals)
            .Include(x => x.Vendor)
            .Include(x => x.ExpenseCategory)
            .Include(x => x.Department)
            .Include(x => x.BusinessUnit)
            .Include(x => x.Budget)
            .Include(x => x.Workflow)
            .FirstOrDefaultAsync(x => x.ExpenseRequestId == expenseRequestId);

        if (e is null)
            return null;

        var userIds = new HashSet<Guid> { e.SubmittedByUserId };
        foreach (var a in e.Approvals)
            userIds.Add(a.ApproverUserId);

        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        users.TryGetValue(e.SubmittedByUserId, out var sub);

        var lineDtos = e.Lines.OrderBy(x => x.SequenceOrder).Select(l => new ExpenseRequestLineResponse(
            l.ExpenseRequestLineId,
            l.SequenceOrder,
            l.Description,
            l.Amount,
            l.ExpenseCategoryId,
            l.LineExpenseCategory?.Name,
            l.GlAccountId,
            l.GlAccount?.Code,
            l.GlAccount?.Name,
            l.VendorId,
            l.LineVendor?.Name)).ToList();

        var apprDtos = e.Approvals.OrderBy(x => x.StepSequence).Select(a =>
        {
            users.TryGetValue(a.ApproverUserId, out var u);
            return new ExpenseRequestApprovalResponse(
                a.ExpenseRequestApprovalId,
                a.StepSequence,
                a.ApproverUserId,
                u?.Email,
                u?.FullName,
                a.StepStatus,
                a.ActionAtUtc,
                a.Comment);
        }).ToList();

        return new ExpenseRequestDetailResponse(
            e.ExpenseRequestId,
            e.RequestNumber,
            e.Title,
            e.Description,
            e.ExpenseType,
            e.ProjectId,
            e.FundingType,
            e.Status,
            e.TotalAmount,
            e.CurrencyCode,
            e.SubmittedByUserId,
            sub?.Email,
            sub?.FullName,
            e.VendorId,
            e.Vendor?.Name,
            e.ExpenseCategoryId,
            e.ExpenseCategory?.Name,
            e.DepartmentId,
            e.Department?.Name,
            e.BusinessUnitId,
            e.BusinessUnit?.Name,
            e.BudgetId,
            e.Budget?.Name,
            e.WorkflowId,
            e.Workflow?.Name,
            e.CurrentApprovalStepSequence,
            e.SubmittedAtUtc,
            e.ApprovedAtUtc,
            e.RejectedAtUtc,
            e.CompletedAtUtc,
            lineDtos,
            apprDtos,
            e.CreatedAtUtc,
            e.UpdatedAtUtc);
    }
}
