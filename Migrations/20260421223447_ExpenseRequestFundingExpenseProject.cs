using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpenseRequestFundingExpenseProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpenseType",
                table: "ExpenseRequests",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "SelfPaidReimbursement");

            migrationBuilder.AddColumn<string>(
                name: "FundingType",
                table: "ExpenseRequests",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "BudgetedExpense");

            migrationBuilder.AddColumn<string>(
                name: "ProjectId",
                table: "ExpenseRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpenseType",
                table: "ExpenseRequests");

            migrationBuilder.DropColumn(
                name: "FundingType",
                table: "ExpenseRequests");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ExpenseRequests");
        }
    }
}
