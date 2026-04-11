using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpandVendorsCrud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Vendors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Vendors SET Code = CONCAT('V', REPLACE(CAST(VendorId AS NVARCHAR(36)), '-', ''))
                WHERE Code IS NULL OR LTRIM(RTRIM(Code)) = '';
                """);

            migrationBuilder.Sql("ALTER TABLE Vendors ALTER COLUMN Code NVARCHAR(50) NOT NULL;");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Vendors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Vendors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Vendors",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Vendors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "Vendors",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultGlAccountId",
                table: "Vendors",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Vendors",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Vendors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Vendors",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermsDays",
                table: "Vendors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Vendors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Vendors",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateRegion",
                table: "Vendors",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxIdentifier",
                table: "Vendors",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Vendors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_DefaultGlAccountId",
                table: "Vendors",
                column: "DefaultGlAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_TenantId_Code",
                table: "Vendors",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_TenantId_IsActive",
                table: "Vendors",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_GlAccounts_DefaultGlAccountId",
                table: "Vendors",
                column: "DefaultGlAccountId",
                principalTable: "GlAccounts",
                principalColumn: "GlAccountId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_GlAccounts_DefaultGlAccountId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_DefaultGlAccountId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_TenantId_Code",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_TenantId_IsActive",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "DefaultGlAccountId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PaymentTermsDays",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "StateRegion",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "TaxIdentifier",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Vendors");
        }
    }
}
