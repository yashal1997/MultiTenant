using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseRequests",
                columns: table => new
                {
                    ExpenseRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentApprovalStepSequence = table.Column<int>(type: "int", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseRequests", x => x.ExpenseRequestId);
                    table.ForeignKey(
                        name: "FK_ExpenseRequests_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseRequests_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "BudgetId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExpenseRequests_BusinessUnits_BusinessUnitId",
                        column: x => x.BusinessUnitId,
                        principalTable: "BusinessUnits",
                        principalColumn: "BusinessUnitId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExpenseRequests_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExpenseRequests_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "ExpenseCategoryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExpenseRequests_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExpenseRequests_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseRequestSequences",
                columns: table => new
                {
                    ExpenseRequestSequenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseRequestSequences", x => x.ExpenseRequestSequenceId);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseRequestApprovals",
                columns: table => new
                {
                    ExpenseRequestApprovalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepSequence = table.Column<int>(type: "int", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActionAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseRequestApprovals", x => x.ExpenseRequestApprovalId);
                    table.ForeignKey(
                        name: "FK_ExpenseRequestApprovals_AspNetUsers_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseRequestApprovals_ExpenseRequests_ExpenseRequestId",
                        column: x => x.ExpenseRequestId,
                        principalTable: "ExpenseRequests",
                        principalColumn: "ExpenseRequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseRequestLines",
                columns: table => new
                {
                    ExpenseRequestLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GlAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseRequestLines", x => x.ExpenseRequestLineId);
                    table.ForeignKey(
                        name: "FK_ExpenseRequestLines_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "ExpenseCategoryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExpenseRequestLines_ExpenseRequests_ExpenseRequestId",
                        column: x => x.ExpenseRequestId,
                        principalTable: "ExpenseRequests",
                        principalColumn: "ExpenseRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpenseRequestLines_GlAccounts_GlAccountId",
                        column: x => x.GlAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "GlAccountId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExpenseRequestLines_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestApprovals_ApproverUserId",
                table: "ExpenseRequestApprovals",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestApprovals_ExpenseRequestId_StepSequence",
                table: "ExpenseRequestApprovals",
                columns: new[] { "ExpenseRequestId", "StepSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestApprovals_TenantId_ExpenseRequestId",
                table: "ExpenseRequestApprovals",
                columns: new[] { "TenantId", "ExpenseRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestLines_ExpenseCategoryId",
                table: "ExpenseRequestLines",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestLines_ExpenseRequestId_SequenceOrder",
                table: "ExpenseRequestLines",
                columns: new[] { "ExpenseRequestId", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestLines_GlAccountId",
                table: "ExpenseRequestLines",
                column: "GlAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestLines_TenantId_ExpenseRequestId",
                table: "ExpenseRequestLines",
                columns: new[] { "TenantId", "ExpenseRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestLines_VendorId",
                table: "ExpenseRequestLines",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_BudgetId",
                table: "ExpenseRequests",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_BusinessUnitId",
                table: "ExpenseRequests",
                column: "BusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_DepartmentId",
                table: "ExpenseRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_ExpenseCategoryId",
                table: "ExpenseRequests",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_SubmittedByUserId",
                table: "ExpenseRequests",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_TenantId_ExpenseRequestId",
                table: "ExpenseRequests",
                columns: new[] { "TenantId", "ExpenseRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_TenantId_IsActive",
                table: "ExpenseRequests",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_TenantId_RequestNumber",
                table: "ExpenseRequests",
                columns: new[] { "TenantId", "RequestNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_TenantId_Status",
                table: "ExpenseRequests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_TenantId_SubmittedByUserId",
                table: "ExpenseRequests",
                columns: new[] { "TenantId", "SubmittedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_VendorId",
                table: "ExpenseRequests",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequests_WorkflowId",
                table: "ExpenseRequests",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRequestSequences_TenantId_Year",
                table: "ExpenseRequestSequences",
                columns: new[] { "TenantId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseRequestApprovals");

            migrationBuilder.DropTable(
                name: "ExpenseRequestLines");

            migrationBuilder.DropTable(
                name: "ExpenseRequestSequences");

            migrationBuilder.DropTable(
                name: "ExpenseRequests");
        }
    }
}
