using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class Release4BudgetAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusinessUnitId",
                table: "Budgets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE b
                SET b.BusinessUnitId = src.BusinessUnitId
                FROM Budgets b
                CROSS APPLY (
                    SELECT TOP 1 bl.BusinessUnitId
                    FROM BudgetLines bl
                    WHERE bl.BudgetId = b.BudgetId
                      AND bl.BusinessUnitId IS NOT NULL
                    ORDER BY bl.SequenceOrder
                ) src
                WHERE b.BusinessUnitId IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE b
                SET b.BusinessUnitId = src.BusinessUnitId
                FROM Budgets b
                CROSS APPLY (
                    SELECT TOP 1 bu.BusinessUnitId
                    FROM BusinessUnits bu
                    WHERE bu.TenantId = b.TenantId
                    ORDER BY bu.CreatedAtUtc, bu.BusinessUnitId
                ) src
                WHERE b.BusinessUnitId IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE bl
                SET bl.BusinessUnitId = b.BusinessUnitId
                FROM BudgetLines bl
                INNER JOIN Budgets b ON b.BudgetId = bl.BudgetId
                WHERE bl.BusinessUnitId IS NULL
                  AND b.BusinessUnitId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                DELETE bl
                FROM BudgetLines bl
                WHERE bl.DepartmentId IS NULL
                   OR bl.BusinessUnitId IS NULL
                   OR bl.ExpenseCategoryId IS NULL;
                """);

            migrationBuilder.Sql("""
                ;WITH DuplicateLines AS (
                    SELECT
                        BudgetLineId,
                        ROW_NUMBER() OVER (
                            PARTITION BY BudgetId, DepartmentId, ExpenseCategoryId
                            ORDER BY SequenceOrder, BudgetLineId
                        ) AS RowNum
                    FROM BudgetLines
                )
                DELETE FROM DuplicateLines
                WHERE RowNum > 1;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessUnitId",
                table: "Budgets",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ExpenseCategoryId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessUnitId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_BusinessUnitId",
                table: "Budgets",
                column: "BusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_BusinessUnitId",
                table: "Budgets",
                columns: new[] { "TenantId", "BusinessUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId_DepartmentId_ExpenseCategoryId",
                table: "BudgetLines",
                columns: new[] { "BudgetId", "DepartmentId", "ExpenseCategoryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_BusinessUnits_BusinessUnitId",
                table: "Budgets",
                column: "BusinessUnitId",
                principalTable: "BusinessUnits",
                principalColumn: "BusinessUnitId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_BusinessUnits_BusinessUnitId",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_BusinessUnitId",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_TenantId_BusinessUnitId",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_BudgetLines_BudgetId_DepartmentId_ExpenseCategoryId",
                table: "BudgetLines");

            migrationBuilder.AlterColumn<Guid>(
                name: "ExpenseCategoryId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessUnitId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessUnitId",
                table: "Budgets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.DropColumn(
                name: "BusinessUnitId",
                table: "Budgets");
        }
    }
}
