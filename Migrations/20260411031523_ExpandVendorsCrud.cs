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
            migrationBuilder.Sql("""
                IF COL_LENGTH('Vendors', 'Code') IS NULL
                BEGIN
                    ALTER TABLE Vendors ADD Code NVARCHAR(50) NULL;
                END
                """);

            migrationBuilder.Sql("""
                UPDATE Vendors SET Code = CONCAT('V', REPLACE(CAST(VendorId AS NVARCHAR(36)), '-', ''))
                WHERE Code IS NULL OR LTRIM(RTRIM(Code)) = '';
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Vendors', 'Code') IS NOT NULL
                BEGIN
                    ALTER TABLE Vendors ALTER COLUMN Code NVARCHAR(50) NOT NULL;
                END
                """);

            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'AddressLine1') IS NULL ALTER TABLE Vendors ADD AddressLine1 NVARCHAR(200) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'AddressLine2') IS NULL ALTER TABLE Vendors ADD AddressLine2 NVARCHAR(200) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'City') IS NULL ALTER TABLE Vendors ADD City NVARCHAR(120) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Country') IS NULL ALTER TABLE Vendors ADD Country NVARCHAR(100) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'DefaultCurrency') IS NULL ALTER TABLE Vendors ADD DefaultCurrency NVARCHAR(3) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'DefaultGlAccountId') IS NULL ALTER TABLE Vendors ADD DefaultGlAccountId UNIQUEIDENTIFIER NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Email') IS NULL ALTER TABLE Vendors ADD Email NVARCHAR(320) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'IsActive') IS NULL ALTER TABLE Vendors ADD IsActive BIT NOT NULL CONSTRAINT DF_Vendors_IsActive DEFAULT(1);");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'LegalName') IS NULL ALTER TABLE Vendors ADD LegalName NVARCHAR(200) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Notes') IS NULL ALTER TABLE Vendors ADD Notes NVARCHAR(2000) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'PaymentTermsDays') IS NULL ALTER TABLE Vendors ADD PaymentTermsDays INT NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Phone') IS NULL ALTER TABLE Vendors ADD Phone NVARCHAR(50) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'PostalCode') IS NULL ALTER TABLE Vendors ADD PostalCode NVARCHAR(30) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'StateRegion') IS NULL ALTER TABLE Vendors ADD StateRegion NVARCHAR(120) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'TaxIdentifier') IS NULL ALTER TABLE Vendors ADD TaxIdentifier NVARCHAR(80) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'UpdatedAtUtc') IS NULL ALTER TABLE Vendors ADD UpdatedAtUtc DATETIME2 NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Website') IS NULL ALTER TABLE Vendors ADD Website NVARCHAR(500) NULL;");

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Vendors_DefaultGlAccountId'
                      AND object_id = OBJECT_ID('Vendors')
                )
                BEGIN
                    CREATE INDEX IX_Vendors_DefaultGlAccountId ON Vendors (DefaultGlAccountId);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Vendors_TenantId_Code'
                      AND object_id = OBJECT_ID('Vendors')
                )
                BEGIN
                    CREATE UNIQUE INDEX IX_Vendors_TenantId_Code ON Vendors (TenantId, Code);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Vendors_TenantId_IsActive'
                      AND object_id = OBJECT_ID('Vendors')
                )
                BEGIN
                    CREATE INDEX IX_Vendors_TenantId_IsActive ON Vendors (TenantId, IsActive);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Vendors_GlAccounts_DefaultGlAccountId'
                )
                BEGIN
                    ALTER TABLE Vendors
                    ADD CONSTRAINT FK_Vendors_GlAccounts_DefaultGlAccountId
                    FOREIGN KEY (DefaultGlAccountId) REFERENCES GlAccounts (GlAccountId)
                    ON DELETE SET NULL;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Vendors_GlAccounts_DefaultGlAccountId') ALTER TABLE Vendors DROP CONSTRAINT FK_Vendors_GlAccounts_DefaultGlAccountId;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vendors_DefaultGlAccountId' AND object_id = OBJECT_ID('Vendors')) DROP INDEX IX_Vendors_DefaultGlAccountId ON Vendors;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vendors_TenantId_Code' AND object_id = OBJECT_ID('Vendors')) DROP INDEX IX_Vendors_TenantId_Code ON Vendors;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vendors_TenantId_IsActive' AND object_id = OBJECT_ID('Vendors')) DROP INDEX IX_Vendors_TenantId_IsActive ON Vendors;");

            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'AddressLine1') IS NOT NULL ALTER TABLE Vendors DROP COLUMN AddressLine1;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'AddressLine2') IS NOT NULL ALTER TABLE Vendors DROP COLUMN AddressLine2;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'City') IS NOT NULL ALTER TABLE Vendors DROP COLUMN City;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Code') IS NOT NULL ALTER TABLE Vendors DROP COLUMN Code;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Country') IS NOT NULL ALTER TABLE Vendors DROP COLUMN Country;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'DefaultCurrency') IS NOT NULL ALTER TABLE Vendors DROP COLUMN DefaultCurrency;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'DefaultGlAccountId') IS NOT NULL ALTER TABLE Vendors DROP COLUMN DefaultGlAccountId;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Email') IS NOT NULL ALTER TABLE Vendors DROP COLUMN Email;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'IsActive') IS NOT NULL ALTER TABLE Vendors DROP COLUMN IsActive;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'LegalName') IS NOT NULL ALTER TABLE Vendors DROP COLUMN LegalName;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Notes') IS NOT NULL ALTER TABLE Vendors DROP COLUMN Notes;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'PaymentTermsDays') IS NOT NULL ALTER TABLE Vendors DROP COLUMN PaymentTermsDays;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Phone') IS NOT NULL ALTER TABLE Vendors DROP COLUMN Phone;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'PostalCode') IS NOT NULL ALTER TABLE Vendors DROP COLUMN PostalCode;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'StateRegion') IS NOT NULL ALTER TABLE Vendors DROP COLUMN StateRegion;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'TaxIdentifier') IS NOT NULL ALTER TABLE Vendors DROP COLUMN TaxIdentifier;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'UpdatedAtUtc') IS NOT NULL ALTER TABLE Vendors DROP COLUMN UpdatedAtUtc;");
            migrationBuilder.Sql("IF COL_LENGTH('Vendors', 'Website') IS NOT NULL ALTER TABLE Vendors DROP COLUMN Website;");
        }
    }
}
