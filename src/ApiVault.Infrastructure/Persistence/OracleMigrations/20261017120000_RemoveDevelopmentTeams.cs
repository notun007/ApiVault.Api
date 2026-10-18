using ApiVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations;

[DbContext(typeof(ApiVaultDbContext))]
[Migration("20261017120000_RemoveDevelopmentTeams")]
public sealed class RemoveDevelopmentTeams : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_API_ASSET_DEV_TEAM_DevelopmentTeamId",
            table: "API_ASSET");

        migrationBuilder.DropIndex(
            name: "IX_API_ASSET_DevelopmentTeamId",
            table: "API_ASSET");

        migrationBuilder.DropColumn(
            name: "DevelopmentTeamId",
            table: "API_ASSET");

        migrationBuilder.DropTable(
            name: "DEV_TEAM");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DEV_TEAM",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                Code = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                ContactEmail = table.Column<string>(type: "NVARCHAR2(320)", maxLength: 320, nullable: true),
                Description = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_DEV_TEAM", x => x.Id));

        migrationBuilder.InsertData(
            table: "DEV_TEAM",
            columns: ["Id", "Code", "Name", "CreatedAtUtc", "CreatedBy"],
            values: [Guid.Empty, "UNASSIGNED", "Unassigned", new DateTime(2026, 10, 17, 0, 0, 0, DateTimeKind.Utc), "migration:RemoveDevelopmentTeams"]);

        migrationBuilder.CreateIndex(
            name: "IX_DEV_TEAM_Code",
            table: "DEV_TEAM",
            column: "Code",
            unique: true);

        migrationBuilder.AddColumn<Guid>(
            name: "DevelopmentTeamId",
            table: "API_ASSET",
            type: "RAW(16)",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.CreateIndex(
            name: "IX_API_ASSET_DevelopmentTeamId",
            table: "API_ASSET",
            column: "DevelopmentTeamId");

        migrationBuilder.AddForeignKey(
            name: "FK_API_ASSET_DEV_TEAM_DevelopmentTeamId",
            table: "API_ASSET",
            column: "DevelopmentTeamId",
            principalTable: "DEV_TEAM",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
