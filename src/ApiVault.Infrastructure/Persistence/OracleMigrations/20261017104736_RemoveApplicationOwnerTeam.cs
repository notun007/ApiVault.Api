using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations
{
    /// <inheritdoc />
    public partial class RemoveApplicationOwnerTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PROJECT_REGISTRY_DEV_TEAM_OwnerTeamId",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropIndex(
                name: "IX_PROJECT_REGISTRY_OwnerTeamId",
                table: "PROJECT_REGISTRY");

            migrationBuilder.DropColumn(
                name: "OwnerTeamId",
                table: "PROJECT_REGISTRY");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerTeamId",
                table: "PROJECT_REGISTRY",
                type: "RAW(16)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_REGISTRY_OwnerTeamId",
                table: "PROJECT_REGISTRY",
                column: "OwnerTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_PROJECT_REGISTRY_DEV_TEAM_OwnerTeamId",
                table: "PROJECT_REGISTRY",
                column: "OwnerTeamId",
                principalTable: "DEV_TEAM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
