using ApiVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations;

[DbContext(typeof(ApiVaultDbContext))]
[Migration("20261015100000_AddEndpointExecutionOverrides")]
public sealed class AddEndpointExecutionOverrides : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "MaxRequestBytes",
            table: "API_ENDPOINT",
            type: "NUMBER(10)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MaxResponseBytes",
            table: "API_ENDPOINT",
            type: "NUMBER(10)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "TimeoutSeconds",
            table: "API_ENDPOINT",
            type: "NUMBER(10)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MaxRequestBytes",
            table: "API_ENDPOINT");

        migrationBuilder.DropColumn(
            name: "MaxResponseBytes",
            table: "API_ENDPOINT");

        migrationBuilder.DropColumn(
            name: "TimeoutSeconds",
            table: "API_ENDPOINT");
    }
}
