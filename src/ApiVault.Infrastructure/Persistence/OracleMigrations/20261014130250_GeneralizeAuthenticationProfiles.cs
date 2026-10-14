using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations
{
    /// <inheritdoc />
    public partial class GeneralizeAuthenticationProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TokenEndpointId",
                table: "API_AUTH_PROFILE",
                type: "RAW(16)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "RAW(16)");

            migrationBuilder.AddColumn<string>(
                name: "ConfigurationJson",
                table: "API_AUTH_PROFILE",
                type: "CLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "API_AUTH_PROFILE",
                type: "NVARCHAR2(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "TokenEndpoint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigurationJson",
                table: "API_AUTH_PROFILE");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "API_AUTH_PROFILE");

            migrationBuilder.AlterColumn<Guid>(
                name: "TokenEndpointId",
                table: "API_AUTH_PROFILE",
                type: "RAW(16)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "RAW(16)",
                oldNullable: true);
        }
    }
}
