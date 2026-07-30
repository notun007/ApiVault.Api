using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedSecurityRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO [SEC_ROLE] ([Id], [Code], [Name], [Description], [IsSystemRole], [IsActive], [CreatedAtUtc], [CreatedBy])
                SELECT NEWID(), v.[Code], v.[Name], v.[Description], 1, 1, SYSUTCDATETIME(), 'migration:SeedSecurityRoles'
                FROM (VALUES
                    ('ADMIN', 'Administrator', 'Full access to the ApiVault administration area'),
                    ('API_OWNER', 'API Owner', 'Own and maintain API catalogue records'),
                    ('TESTER', 'Tester', 'Execute controlled API tests'),
                    ('VIEWER', 'Viewer', 'Read-only access to governed catalogue data'),
                    ('SECURITY_ADMIN', 'Security Administrator', 'Manage roles and screen permissions')) v([Code], [Name], [Description])
                WHERE NOT EXISTS (SELECT 1 FROM [SEC_ROLE] r WHERE r.[Code] = v.[Code]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [SEC_ROLE]
                WHERE [Code] IN ('ADMIN', 'API_OWNER', 'TESTER', 'VIEWER', 'SECURITY_ADMIN');");
        }
    }
}
