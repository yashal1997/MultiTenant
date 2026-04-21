using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MultiTenant.Api.Infrastructure.Persistence;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260417170000_UpdateBusinessUnitsForRelease2")]
    public partial class UpdateBusinessUnitsForRelease2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessUnits_TenantId_DepartmentId_Name",
                table: "BusinessUnits");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "BusinessUnits",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "HeadOfUnitUserId",
                table: "BusinessUnits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitCode",
                table: "BusinessUnits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                ;WITH UnitCodes AS (
                    SELECT
                        BusinessUnitId,
                        TenantId,
                        BaseCode =
                            UPPER(
                                LEFT(
                                    REPLACE(REPLACE(REPLACE(ISNULL(Name, 'BU'), ' ', ''), '-', ''), '&', ''),
                                    4
                                )
                            ),
                        RowNum = ROW_NUMBER() OVER (
                            PARTITION BY TenantId,
                            UPPER(
                                LEFT(
                                    REPLACE(REPLACE(REPLACE(ISNULL(Name, 'BU'), ' ', ''), '-', ''), '&', ''),
                                    4
                                )
                            )
                            ORDER BY CreatedAtUtc, BusinessUnitId
                        )
                    FROM BusinessUnits
                )
                UPDATE bu
                SET UnitCode =
                    CASE
                        WHEN uc.RowNum = 1 THEN NULLIF(uc.BaseCode, '')
                        ELSE CONCAT(NULLIF(uc.BaseCode, ''), uc.RowNum)
                    END
                FROM BusinessUnits bu
                INNER JOIN UnitCodes uc ON uc.BusinessUnitId = bu.BusinessUnitId;

                UPDATE BusinessUnits
                SET UnitCode = 'BU'
                WHERE UnitCode IS NULL OR LTRIM(RTRIM(UnitCode)) = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "UnitCode",
                table: "BusinessUnits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_HeadOfUnitUserId",
                table: "BusinessUnits",
                column: "HeadOfUnitUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_TenantId_Name",
                table: "BusinessUnits",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_TenantId_UnitCode",
                table: "BusinessUnits",
                columns: new[] { "TenantId", "UnitCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessUnits_AspNetUsers_HeadOfUnitUserId",
                table: "BusinessUnits",
                column: "HeadOfUnitUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessUnits_AspNetUsers_HeadOfUnitUserId",
                table: "BusinessUnits");

            migrationBuilder.DropIndex(
                name: "IX_BusinessUnits_HeadOfUnitUserId",
                table: "BusinessUnits");

            migrationBuilder.DropIndex(
                name: "IX_BusinessUnits_TenantId_Name",
                table: "BusinessUnits");

            migrationBuilder.DropIndex(
                name: "IX_BusinessUnits_TenantId_UnitCode",
                table: "BusinessUnits");

            migrationBuilder.DropColumn(
                name: "HeadOfUnitUserId",
                table: "BusinessUnits");

            migrationBuilder.DropColumn(
                name: "UnitCode",
                table: "BusinessUnits");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "BusinessUnits",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_TenantId_DepartmentId_Name",
                table: "BusinessUnits",
                columns: new[] { "TenantId", "DepartmentId", "Name" },
                unique: true);
        }
    }
}
