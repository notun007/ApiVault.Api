using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUserSecurityRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO [APP_USER_ROLE] ([Id], [UserId], [RoleId], [CreatedAtUtc], [CreatedBy])
                SELECT NEWID(), u.[Id], r.[Id], SYSUTCDATETIME(), 'migration:BackfillUserSecurityRoles'
                FROM [APP_USER] u
                INNER JOIN [SEC_ROLE] r ON r.[Code] = CASE u.[Role]
                    WHEN 'Admin' THEN 'ADMIN'
                    WHEN 'ApiOwner' THEN 'API_OWNER'
                    WHEN 'Tester' THEN 'TESTER'
                    WHEN 'Viewer' THEN 'VIEWER'
                END
                WHERE NOT EXISTS (
                    SELECT 1 FROM [APP_USER_ROLE] ur
                    WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [APP_USER_ROLE]
                WHERE [CreatedBy] = 'migration:BackfillUserSecurityRoles';");
        }
    }
}
