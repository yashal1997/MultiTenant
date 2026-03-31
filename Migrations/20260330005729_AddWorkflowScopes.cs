using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workflows_Departments_DepartmentId",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Workflows_DepartmentId",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Workflows_TenantId_DepartmentId",
                table: "Workflows");

            migrationBuilder.AddColumn<bool>(
                name: "ApplyToAllBusinessUnits",
                table: "Workflows",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ApplyToAllDepartments",
                table: "Workflows",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ApplyToAllExpenseCategories",
                table: "Workflows",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "WorkflowBusinessUnitScopes",
                columns: table => new
                {
                    WorkflowBusinessUnitScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowBusinessUnitScopes", x => x.WorkflowBusinessUnitScopeId);
                    table.ForeignKey(
                        name: "FK_WorkflowBusinessUnitScopes_BusinessUnits_BusinessUnitId",
                        column: x => x.BusinessUnitId,
                        principalTable: "BusinessUnits",
                        principalColumn: "BusinessUnitId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowBusinessUnitScopes_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDepartmentScopes",
                columns: table => new
                {
                    WorkflowDepartmentScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDepartmentScopes", x => x.WorkflowDepartmentScopeId);
                    table.ForeignKey(
                        name: "FK_WorkflowDepartmentScopes_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowDepartmentScopes_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExpenseCategoryScopes",
                columns: table => new
                {
                    WorkflowExpenseCategoryScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExpenseCategoryScopes", x => x.WorkflowExpenseCategoryScopeId);
                    table.ForeignKey(
                        name: "FK_WorkflowExpenseCategoryScopes_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "ExpenseCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowExpenseCategoryScopes_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO WorkflowDepartmentScopes (WorkflowDepartmentScopeId, TenantId, WorkflowId, DepartmentId)
                SELECT NEWID(), TenantId, WorkflowId, DepartmentId
                FROM Workflows
                WHERE DepartmentId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE Workflows SET ApplyToAllDepartments = 0 WHERE DepartmentId IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Workflows");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowBusinessUnitScopes_BusinessUnitId",
                table: "WorkflowBusinessUnitScopes",
                column: "BusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowBusinessUnitScopes_TenantId_WorkflowId_BusinessUnitId",
                table: "WorkflowBusinessUnitScopes",
                columns: new[] { "TenantId", "WorkflowId", "BusinessUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowBusinessUnitScopes_WorkflowId",
                table: "WorkflowBusinessUnitScopes",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDepartmentScopes_DepartmentId",
                table: "WorkflowDepartmentScopes",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDepartmentScopes_TenantId_WorkflowId_DepartmentId",
                table: "WorkflowDepartmentScopes",
                columns: new[] { "TenantId", "WorkflowId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDepartmentScopes_WorkflowId",
                table: "WorkflowDepartmentScopes",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExpenseCategoryScopes_ExpenseCategoryId",
                table: "WorkflowExpenseCategoryScopes",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExpenseCategoryScopes_TenantId_WorkflowId_ExpenseCategoryId",
                table: "WorkflowExpenseCategoryScopes",
                columns: new[] { "TenantId", "WorkflowId", "ExpenseCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExpenseCategoryScopes_WorkflowId",
                table: "WorkflowExpenseCategoryScopes",
                column: "WorkflowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Workflows",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE w SET w.DepartmentId = s.DepartmentId
                FROM Workflows w
                INNER JOIN (
                    SELECT WorkflowId, MIN(DepartmentId) AS DepartmentId
                    FROM WorkflowDepartmentScopes
                    GROUP BY WorkflowId
                ) s ON w.WorkflowId = s.WorkflowId;
                """);

            migrationBuilder.DropTable(
                name: "WorkflowBusinessUnitScopes");

            migrationBuilder.DropTable(
                name: "WorkflowDepartmentScopes");

            migrationBuilder.DropTable(
                name: "WorkflowExpenseCategoryScopes");

            migrationBuilder.DropColumn(
                name: "ApplyToAllBusinessUnits",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "ApplyToAllDepartments",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "ApplyToAllExpenseCategories",
                table: "Workflows");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_DepartmentId",
                table: "Workflows",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_TenantId_DepartmentId",
                table: "Workflows",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Workflows_Departments_DepartmentId",
                table: "Workflows",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
