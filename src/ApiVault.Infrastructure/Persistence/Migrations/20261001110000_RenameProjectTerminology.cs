using ApiVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApiVaultDbContext))]
[Migration("20261001110000_RenameProjectTerminology")]
public sealed class RenameProjectTerminology : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            UPDATE [SEC_SCREEN]
            SET [Name] = CASE [Code]
                WHEN 'PROJECTS' THEN 'Consumer Applications'
                WHEN 'API_PROJECTS' THEN 'API Source Systems'
                ELSE [Name]
            END
            WHERE [Code] IN ('PROJECTS', 'API_PROJECTS');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            UPDATE [SEC_SCREEN]
            SET [Name] = CASE [Code]
                WHEN 'PROJECTS' THEN 'Projects'
                WHEN 'API_PROJECTS' THEN 'API Projects'
                ELSE [Name]
            END
            WHERE [Code] IN ('PROJECTS', 'API_PROJECTS');");
    }
}
