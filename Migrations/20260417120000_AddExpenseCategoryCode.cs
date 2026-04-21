using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MultiTenant.Api.Infrastructure.Persistence;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260417120000_AddExpenseCategoryCode")]
    public partial class AddExpenseCategoryCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryCode",
                table: "ExpenseCategories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE ec
                SET [CategoryCode] = LEFT(
                    (
                        SELECT STRING_AGG(LEFT(value, 1), '')
                        FROM STRING_SPLIT(ec.[Name], ' ')
                        WHERE LTRIM(RTRIM(value)) <> ''
                    ),
                    3
                )
                FROM [ExpenseCategories] ec
                WHERE ec.[CategoryCode] = '';
                """);

            migrationBuilder.Sql("""
                WITH cte AS (
                    SELECT
                        [ExpenseCategoryId],
                        [TenantId],
                        [CategoryCode],
                        ROW_NUMBER() OVER (PARTITION BY [TenantId], [CategoryCode] ORDER BY [CreatedAtUtc], [ExpenseCategoryId]) AS rn
                    FROM [ExpenseCategories]
                )
                UPDATE ec
                SET [CategoryCode] = LEFT(cte.[CategoryCode], 47) + RIGHT('00' + CAST(cte.rn AS varchar(2)), 2)
                FROM [ExpenseCategories] ec
                INNER JOIN cte ON cte.[ExpenseCategoryId] = ec.[ExpenseCategoryId]
                WHERE cte.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_TenantId_CategoryCode",
                table: "ExpenseCategories",
                columns: new[] { "TenantId", "CategoryCode" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_TenantId_CategoryCode",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "CategoryCode",
                table: "ExpenseCategories");
        }
    }
}
