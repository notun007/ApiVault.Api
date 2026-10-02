using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateApplicationRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_API_ASSET_PROJECT_REGISTRY_ApiProjectId",
                table: "API_ASSET");

            migrationBuilder.DropColumn(
                name: "ConsumesApis",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropColumn(
                name: "PublishesApis",
                table: "PROJECT_REGISTRY");

            migrationBuilder.RenameColumn(
                name: "ApiProjectId",
                table: "API_ASSET",
                newName: "PublishingApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_API_ASSET_Name_ApiProjectId",
                table: "API_ASSET",
                newName: "IX_API_ASSET_Name_PublishingApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_API_ASSET_ApiProjectId",
                table: "API_ASSET",
                newName: "IX_API_ASSET_PublishingApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_API_ASSET_PROJECT_REGISTRY_PublishingApplicationId",
                table: "API_ASSET",
                column: "PublishingApplicationId",
                principalTable: "PROJECT_REGISTRY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                DELETE rp
                FROM [ROLE_PERMISSION] rp
                INNER JOIN [SEC_SCREEN] s ON s.[Id] = rp.[ScreenId]
                WHERE s.[Code] = 'API_PROJECTS';

                DELETE FROM [SEC_SCREEN] WHERE [Code] = 'API_PROJECTS';

                UPDATE [SEC_SCREEN]
                SET [Name] = 'Applications & Systems', [Route] = '/projects', [Icon] = 'AS'
                WHERE [Code] = 'PROJECTS';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_API_ASSET_PROJECT_REGISTRY_PublishingApplicationId",
                table: "API_ASSET");

            migrationBuilder.RenameColumn(
                name: "PublishingApplicationId",
                table: "API_ASSET",
                newName: "ApiProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_API_ASSET_PublishingApplicationId",
                table: "API_ASSET",
                newName: "IX_API_ASSET_ApiProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_API_ASSET_Name_PublishingApplicationId",
                table: "API_ASSET",
                newName: "IX_API_ASSET_Name_ApiProjectId");

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

            migrationBuilder.AddColumn<bool>(
                name: "PublishesApis",
                table: "PROJECT_REGISTRY",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE [PROJECT_REGISTRY]
                SET [IsActive] = CASE WHEN [Status] = 'Active' THEN 1 ELSE 0 END,
                    [PublishesApis] = CASE WHEN EXISTS (
                        SELECT 1 FROM [API_ASSET] a WHERE a.[ApiProjectId] = [PROJECT_REGISTRY].[Id]
                    ) THEN 1 ELSE 0 END,
                    [ConsumesApis] = CASE WHEN EXISTS (
                        SELECT 1 FROM [PROJECT_API_VER] pav WHERE pav.[ProjectId] = [PROJECT_REGISTRY].[Id]
                    ) THEN 1 ELSE 0 END;

                UPDATE [SEC_SCREEN]
                SET [Name] = 'Consumer Applications', [Route] = '/projects', [Icon] = 'PR'
                WHERE [Code] = 'PROJECTS';

                IF NOT EXISTS (SELECT 1 FROM [SEC_SCREEN] WHERE [Code] = 'API_PROJECTS')
                BEGIN
                    INSERT INTO [SEC_SCREEN]
                        ([Id], [Code], [Name], [Route], [Icon], [DisplayOrder], [IsActive], [CreatedAtUtc], [CreatedBy])
                    VALUES
                        (NEWID(), 'API_PROJECTS', 'API Source Systems', '/admin/api-projects', 'PJ', 70, 1, SYSUTCDATETIME(), 'migration:ConsolidateApplicationRegistry:down');
                END;

                INSERT INTO [ROLE_PERMISSION]
                    ([Id], [RoleId], [ScreenId], [PermissionId], [CreatedAtUtc], [CreatedBy])
                SELECT NEWID(), r.[Id], s.[Id], p.[Id], SYSUTCDATETIME(), 'migration:ConsolidateApplicationRegistry:down'
                FROM [SEC_ROLE] r
                CROSS JOIN [SEC_SCREEN] s
                CROSS JOIN [SEC_PERMISSION] p
                WHERE r.[Code] IN ('SUPER_ADMIN', 'ADMIN', 'API_OWNER')
                  AND s.[Code] = 'API_PROJECTS'
                  AND NOT EXISTS (
                      SELECT 1 FROM [ROLE_PERMISSION] rp
                      WHERE rp.[RoleId] = r.[Id] AND rp.[ScreenId] = s.[Id] AND rp.[PermissionId] = p.[Id]
                  );");

            migrationBuilder.AddForeignKey(
                name: "FK_API_ASSET_PROJECT_REGISTRY_ApiProjectId",
                table: "API_ASSET",
                column: "ApiProjectId",
                principalTable: "PROJECT_REGISTRY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
