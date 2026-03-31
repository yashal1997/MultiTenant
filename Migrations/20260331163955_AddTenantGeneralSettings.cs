using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantGeneralSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantGeneralSettings",
                columns: table => new
                {
                    TenantGeneralSettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StateOrProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateFormat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantGeneralSettings", x => x.TenantGeneralSettingId);
                    table.ForeignKey(
                        name: "FK_TenantGeneralSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantGeneralSettings_TenantId",
                table: "TenantGeneralSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO [TenantGeneralSettings]
                    ([TenantGeneralSettingId], [TenantId], [CompanyName], [LegalName], [SupportEmail], [PhoneNumber], [WebsiteUrl], [TaxRegistrationNumber], [AddressLine1], [AddressLine2], [City], [StateOrProvince], [PostalCode], [CountryCode], [CurrencyCode], [TimeZoneId], [DateFormat], [FiscalYearStartMonth], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT
                    NEWID(),
                    t.[TenantId],
                    t.[Name],
                    t.[Name],
                    NULL,
                    NULL,
                    NULL,
                    NULL,
                    NULL,
                    NULL,
                    NULL,
                    NULL,
                    NULL,
                    'US',
                    'USD',
                    'UTC',
                    'yyyy-MM-dd',
                    1,
                    GETUTCDATE(),
                    NULL
                FROM [Tenants] t
                LEFT JOIN [TenantGeneralSettings] gs
                    ON gs.[TenantId] = t.[TenantId]
                WHERE gs.[TenantGeneralSettingId] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantGeneralSettings");
        }
    }
}
