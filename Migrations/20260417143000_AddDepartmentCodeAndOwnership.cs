using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MultiTenant.Api.Infrastructure.Persistence;

#nullable disable

namespace MultiTenant.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260417143000_AddDepartmentCodeAndOwnership")]
    public partial class AddDepartmentCodeAndOwnership : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                table: "Departments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HeadOfDepartmentUserId",
                table: "Departments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrimaryBusinessUnitId",
                table: "Departments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                ;WITH DepartmentCodes AS (
                    SELECT
                        DepartmentId,
                        TenantId,
                        BaseCode =
                            UPPER(
                                LEFT(
                                    REPLACE(REPLACE(REPLACE(ISNULL(Name, 'DEP'), ' ', ''), '-', ''), '&', ''),
                                    3
                                )
                            ),
                        RowNum = ROW_NUMBER() OVER (
                            PARTITION BY TenantId,
                            UPPER(
                                LEFT(
                                    REPLACE(REPLACE(REPLACE(ISNULL(Name, 'DEP'), ' ', ''), '-', ''), '&', ''),
                                    3
                                )
                            )
                            ORDER BY CreatedAtUtc, DepartmentId
                        )
                    FROM Departments
                )
                UPDATE d
                SET DepartmentCode =
                    CASE
                        WHEN dc.RowNum = 1 THEN NULLIF(dc.BaseCode, '')
                        ELSE CONCAT(NULLIF(dc.BaseCode, ''), dc.RowNum)
                    END
                FROM Departments d
                INNER JOIN DepartmentCodes dc ON dc.DepartmentId = d.DepartmentId;

                UPDATE Departments
                SET DepartmentCode = 'DEP'
                WHERE DepartmentCode IS NULL OR LTRIM(RTRIM(DepartmentCode)) = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentCode",
                table: "Departments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadOfDepartmentUserId",
                table: "Departments",
                column: "HeadOfDepartmentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_PrimaryBusinessUnitId",
                table: "Departments",
                column: "PrimaryBusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_DepartmentCode",
                table: "Departments",
                columns: new[] { "TenantId", "DepartmentCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AspNetUsers_HeadOfDepartmentUserId",
                table: "Departments",
                column: "HeadOfDepartmentUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_BusinessUnits_PrimaryBusinessUnitId",
                table: "Departments",
                column: "PrimaryBusinessUnitId",
                principalTable: "BusinessUnits",
                principalColumn: "BusinessUnitId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AspNetUsers_HeadOfDepartmentUserId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_BusinessUnits_PrimaryBusinessUnitId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_HeadOfDepartmentUserId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_PrimaryBusinessUnitId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_TenantId_DepartmentCode",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "HeadOfDepartmentUserId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "PrimaryBusinessUnitId",
                table: "Departments");
        }
    }
}
