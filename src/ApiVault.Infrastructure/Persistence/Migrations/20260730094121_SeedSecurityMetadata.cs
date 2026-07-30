using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedSecurityMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO [SEC_PERMISSION] ([Id], [Code], [Name], [Description], [CreatedAtUtc], [CreatedBy])
                SELECT NEWID(), v.[Code], v.[Name], v.[Description], SYSUTCDATETIME(), 'migration:SeedSecurityMetadata'
                FROM (VALUES
                    ('VIEW', 'View', 'View a screen and its data'),
                    ('CREATE', 'Create', 'Create new records'),
                    ('UPDATE', 'Update', 'Update existing records'),
                    ('DELETE', 'Delete', 'Delete records'),
                    ('EXECUTE', 'Execute', 'Execute controlled operations'),
                    ('APPROVE', 'Approve', 'Approve governed changes')) v([Code], [Name], [Description])
                WHERE NOT EXISTS (SELECT 1 FROM [SEC_PERMISSION] p WHERE p.[Code] = v.[Code]);

                INSERT INTO [SEC_SCREEN] ([Id], [Code], [Name], [Route], [Icon], [DisplayOrder], [IsActive], [CreatedAtUtc], [CreatedBy])
                SELECT NEWID(), v.[Code], v.[Name], v.[Route], v.[Icon], v.[DisplayOrder], 1, SYSUTCDATETIME(), 'migration:SeedSecurityMetadata'
                FROM (VALUES
                    ('DASHBOARD', 'Dashboard', '/dashboard', 'DB', 10),
                    ('API_CATALOG', 'API Catalog', '/apis', 'AP', 20),
                    ('PROJECTS', 'Projects', '/projects', 'PR', 30),
                    ('TEST_CONSOLE', 'Test Console', '/testing', 'TX', 40),
                    ('TEST_HISTORY', 'Test History', '/test-history', 'HS', 50),
                    ('REFERENCE_DATA', 'Reference Data', '/admin/reference-data', 'RF', 60),
                    ('API_PROJECTS', 'API Projects', '/admin/api-projects', 'PJ', 70),
                    ('USERS', 'Users', '/admin/users', 'US', 80),
                    ('SECURITY_ROLES', 'Roles', '/admin/security/roles', 'RL', 90),
                    ('SECURITY_PERMISSIONS', 'Permissions', '/admin/security/permissions', 'PM', 100),
                    ('AUDIT_LOGS', 'Audit Logs', '/admin/audit', 'AU', 110)) v([Code], [Name], [Route], [Icon], [DisplayOrder])
                WHERE NOT EXISTS (SELECT 1 FROM [SEC_SCREEN] s WHERE s.[Code] = v.[Code]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [SEC_SCREEN]
                WHERE [Code] IN ('DASHBOARD', 'API_CATALOG', 'PROJECTS', 'TEST_CONSOLE', 'TEST_HISTORY', 'REFERENCE_DATA', 'API_PROJECTS', 'USERS', 'SECURITY_ROLES', 'SECURITY_PERMISSIONS', 'AUDIT_LOGS');
                DELETE FROM [SEC_PERMISSION]
                WHERE [Code] IN ('VIEW', 'CREATE', 'UPDATE', 'DELETE', 'EXECUTE', 'APPROVE');");
        }
    }
}
