using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnifySystemsAndVendors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_API_ASSET_API_PROJECT_ApiProjectId",
                table: "API_ASSET");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerTeamId",
                table: "PROJECT_REGISTRY",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessAreaId",
                table: "PROJECT_REGISTRY",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "ConsumesApis",
                table: "PROJECT_REGISTRY",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PROJECT_REGISTRY",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipType",
                table: "PROJECT_REGISTRY",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PublishesApis",
                table: "PROJECT_REGISTRY",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorId",
                table: "PROJECT_REGISTRY",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VENDOR",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    SupportPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VENDOR", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_REGISTRY_VendorId",
                table: "PROJECT_REGISTRY",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VENDOR_Code",
                table: "VENDOR",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VENDOR_Name",
                table: "VENDOR",
                column: "Name",
                unique: true);

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM [API_PROJECT] WHERE LEN([Code]) > 50)
                    THROW 50001, 'An API source system code exceeds the new 50-character limit.', 1;

                UPDATE [PROJECT_REGISTRY]
                SET [ConsumesApis] = 1,
                    [IsActive] = CASE WHEN [Status] = 'Inactive' THEN 0 ELSE 1 END,
                    [OwnershipType] = 'Internal';

                ;WITH VendorNames AS (
                    SELECT MIN(LTRIM(RTRIM([VendorName]))) AS [Name]
                    FROM [API_ASSET]
                    WHERE NULLIF(LTRIM(RTRIM([VendorName])), '') IS NOT NULL
                    GROUP BY UPPER(LTRIM(RTRIM([VendorName])))
                ), Numbered AS (
                    SELECT [Name], ROW_NUMBER() OVER (ORDER BY [Name]) AS [RowNumber]
                    FROM VendorNames
                )
                INSERT INTO [VENDOR] ([Id], [Code], [Name], [IsActive], [CreatedAtUtc], [CreatedBy])
                SELECT NEWID(), CONCAT('MIG-', RIGHT(CONCAT('0000', [RowNumber]), 4)), [Name], 1,
                       SYSUTCDATETIME(), 'migration:UnifySystemsAndVendors'
                FROM Numbered;

                INSERT INTO [PROJECT_REGISTRY]
                    ([Id], [Code], [Name], [Description], [Criticality], [Status], [BusinessAreaId], [OwnerTeamId],
                     [OwnershipType], [VendorId], [IsActive], [PublishesApis], [ConsumesApis],
                     [CreatedAtUtc], [CreatedBy], [UpdatedAtUtc], [UpdatedBy])
                SELECT ap.[Id], ap.[Code], ap.[Name], ap.[Description], 'Medium',
                       CASE WHEN ap.[IsActive] = 1 THEN 'Active' ELSE 'Inactive' END,
                       NULL, NULL,
                       CASE WHEN EXISTS (
                           SELECT 1 FROM [API_ASSET] a
                           WHERE a.[ApiProjectId] = ap.[Id] AND a.[OwnershipType] = 'ThirdParty'
                       ) THEN 'ThirdParty' ELSE 'Internal' END,
                       (SELECT TOP 1 v.[Id]
                        FROM [API_ASSET] a
                        JOIN [VENDOR] v ON UPPER(v.[Name]) = UPPER(LTRIM(RTRIM(a.[VendorName])))
                        WHERE a.[ApiProjectId] = ap.[Id]
                        ORDER BY a.[CreatedAtUtc]),
                       ap.[IsActive], 1, 0, ap.[CreatedAtUtc], ap.[CreatedBy], ap.[UpdatedAtUtc], ap.[UpdatedBy]
                FROM [API_PROJECT] ap
                WHERE NOT EXISTS (SELECT 1 FROM [PROJECT_REGISTRY] p WHERE UPPER(p.[Code]) = UPPER(ap.[Code]));

                UPDATE p
                SET p.[PublishesApis] = 1,
                    p.[OwnershipType] = CASE WHEN EXISTS (
                        SELECT 1 FROM [API_ASSET] a
                        JOIN [API_PROJECT] ap2 ON ap2.[Id] = a.[ApiProjectId]
                        WHERE UPPER(ap2.[Code]) = UPPER(p.[Code]) AND a.[OwnershipType] = 'ThirdParty'
                    ) THEN 'ThirdParty' ELSE p.[OwnershipType] END,
                    p.[VendorId] = COALESCE(p.[VendorId], (
                        SELECT TOP 1 v.[Id]
                        FROM [API_ASSET] a
                        JOIN [API_PROJECT] ap2 ON ap2.[Id] = a.[ApiProjectId]
                        JOIN [VENDOR] v ON UPPER(v.[Name]) = UPPER(LTRIM(RTRIM(a.[VendorName])))
                        WHERE UPPER(ap2.[Code]) = UPPER(p.[Code])
                        ORDER BY a.[CreatedAtUtc]
                    ))
                FROM [PROJECT_REGISTRY] p
                WHERE EXISTS (SELECT 1 FROM [API_PROJECT] ap WHERE UPPER(ap.[Code]) = UPPER(p.[Code]));

                UPDATE a
                SET a.[ApiProjectId] = p.[Id]
                FROM [API_ASSET] a
                JOIN [API_PROJECT] ap ON ap.[Id] = a.[ApiProjectId]
                JOIN [PROJECT_REGISTRY] p ON UPPER(p.[Code]) = UPPER(ap.[Code]);");

            migrationBuilder.DropTable(
                name: "API_PROJECT");

            migrationBuilder.AddForeignKey(
                name: "FK_API_ASSET_PROJECT_REGISTRY_ApiProjectId",
                table: "API_ASSET",
                column: "ApiProjectId",
                principalTable: "PROJECT_REGISTRY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PROJECT_REGISTRY_VENDOR_VendorId",
                table: "PROJECT_REGISTRY",
                column: "VendorId",
                principalTable: "VENDOR",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                INSERT INTO [SEC_SCREEN]
                    ([Id], [Code], [Name], [Route], [Icon], [DisplayOrder], [IsActive], [CreatedAtUtc], [CreatedBy])
                SELECT NEWID(), v.[Code], v.[Name], v.[Route], v.[Icon], v.[DisplayOrder], 1,
                       SYSUTCDATETIME(), 'migration:UnifySystemsAndVendors'
                FROM (VALUES
                    ('VENDORS', 'Vendor Companies', '/admin/vendors', 'VN', 75),
                    ('RESET_PASSWORD', 'Reset Password', '/admin/reset-password', 'PW', 85)
                ) v([Code], [Name], [Route], [Icon], [DisplayOrder])
                WHERE NOT EXISTS (SELECT 1 FROM [SEC_SCREEN] s WHERE s.[Code] = v.[Code]);

                INSERT INTO [ROLE_PERMISSION]
                    ([Id], [RoleId], [ScreenId], [PermissionId], [CreatedAtUtc], [CreatedBy])
                SELECT NEWID(), r.[Id], s.[Id], p.[Id], SYSUTCDATETIME(), 'migration:UnifySystemsAndVendors'
                FROM [SEC_ROLE] r
                CROSS JOIN [SEC_SCREEN] s
                CROSS JOIN [SEC_PERMISSION] p
                WHERE r.[Code] IN ('SUPER_ADMIN', 'ADMIN')
                  AND s.[Code] IN ('VENDORS', 'RESET_PASSWORD')
                  AND NOT EXISTS (
                      SELECT 1 FROM [ROLE_PERMISSION] rp
                      WHERE rp.[RoleId] = r.[Id]
                        AND rp.[ScreenId] = s.[Id]
                        AND rp.[PermissionId] = p.[Id]
                  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [ROLE_PERMISSION]
                WHERE [CreatedBy] = 'migration:UnifySystemsAndVendors'
                  AND [ScreenId] IN (SELECT [Id] FROM [SEC_SCREEN] WHERE [Code] IN ('VENDORS', 'RESET_PASSWORD'));

                DELETE FROM [SEC_SCREEN]
                WHERE [CreatedBy] = 'migration:UnifySystemsAndVendors'
                  AND [Code] IN ('VENDORS', 'RESET_PASSWORD');");

            migrationBuilder.DropForeignKey(
                name: "FK_API_ASSET_PROJECT_REGISTRY_ApiProjectId",
                table: "API_ASSET");

            migrationBuilder.DropForeignKey(
                name: "FK_PROJECT_REGISTRY_VENDOR_VendorId",
                table: "PROJECT_REGISTRY");

            migrationBuilder.Sql(@"
                CREATE TABLE [API_PROJECT] (
                    [Id] uniqueidentifier NOT NULL,
                    [Code] nvarchar(100) NOT NULL,
                    [CreatedAtUtc] datetime2(6) NOT NULL,
                    [CreatedBy] nvarchar(200) NOT NULL,
                    [Description] nvarchar(4000) NULL,
                    [IsActive] bit NOT NULL,
                    [Name] nvarchar(200) NOT NULL,
                    [UpdatedAtUtc] datetime2(6) NULL,
                    [UpdatedBy] nvarchar(200) NULL,
                    CONSTRAINT [PK_API_PROJECT] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_API_PROJECT_Code] ON [API_PROJECT] ([Code]);
                CREATE UNIQUE INDEX [IX_API_PROJECT_Name] ON [API_PROJECT] ([Name]);

                INSERT INTO [API_PROJECT]
                    ([Id], [Code], [Name], [Description], [IsActive], [CreatedAtUtc], [CreatedBy], [UpdatedAtUtc], [UpdatedBy])
                SELECT [Id], [Code], [Name], [Description], [IsActive], [CreatedAtUtc], [CreatedBy], [UpdatedAtUtc], [UpdatedBy]
                FROM [PROJECT_REGISTRY]
                WHERE [PublishesApis] = 1;

                DELETE FROM [PROJECT_REGISTRY]
                WHERE [PublishesApis] = 1 AND [ConsumesApis] = 0;");

            migrationBuilder.DropTable(
                name: "VENDOR");

            migrationBuilder.DropIndex(
                name: "IX_PROJECT_REGISTRY_VendorId",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropColumn(
                name: "ConsumesApis",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropColumn(
                name: "PublishesApis",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "PROJECT_REGISTRY");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerTeamId",
                table: "PROJECT_REGISTRY",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessAreaId",
                table: "PROJECT_REGISTRY",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_API_ASSET_API_PROJECT_ApiProjectId",
                table: "API_ASSET",
                column: "ApiProjectId",
                principalTable: "API_PROJECT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
