using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.BudgetId);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                columns: table => new
                {
                    BudgetLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GlAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.BudgetLineId);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "BudgetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetLines_BusinessUnits_BusinessUnitId",
                        column: x => x.BusinessUnitId,
                        principalTable: "BusinessUnits",
                        principalColumn: "BusinessUnitId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "ExpenseCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_GlAccounts_GlAccountId",
                        column: x => x.GlAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "GlAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId",
                table: "BudgetLines",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId_SequenceOrder",
                table: "BudgetLines",
                columns: new[] { "BudgetId", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BusinessUnitId",
                table: "BudgetLines",
                column: "BusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_DepartmentId",
                table: "BudgetLines",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_ExpenseCategoryId",
                table: "BudgetLines",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_GlAccountId",
                table: "BudgetLines",
                column: "GlAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_BudgetId",
                table: "Budgets",
                columns: new[] { "TenantId", "BudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_FiscalYear",
                table: "Budgets",
                columns: new[] { "TenantId", "FiscalYear" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_FiscalYear_Name",
                table: "Budgets",
                columns: new[] { "TenantId", "FiscalYear", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_IsActive",
                table: "Budgets",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_Status",
                table: "Budgets",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetLines");

            migrationBuilder.DropTable(
                name: "Budgets");
        }
    }
}
