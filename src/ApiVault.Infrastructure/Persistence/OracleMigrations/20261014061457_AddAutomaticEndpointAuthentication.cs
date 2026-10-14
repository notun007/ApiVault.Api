using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations
{
    /// <inheritdoc />
    public partial class AddAutomaticEndpointAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AuthenticationApplied",
                table: "TEST_EXECUTION",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthenticationProfileId",
                table: "TEST_EXECUTION",
                type: "RAW(16)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthenticationTokenExpiresAtUtc",
                table: "TEST_EXECUTION",
                type: "TIMESTAMP(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AuthenticationTokenFromCache",
                table: "TEST_EXECUTION",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AuthenticationMode",
                table: "API_ENDPOINT",
                type: "NVARCHAR2(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Inherit");

            migrationBuilder.CreateTable(
                name: "API_AUTH_PROFILE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiVersionId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    TokenEndpointId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TokenJsonPath = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    TokenTypeJsonPath = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    ExpiresAtJsonPath = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    ExpiresInJsonPath = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    HeaderName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    RefreshSkewSeconds = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_API_AUTH_PROFILE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_API_AUTH_PROFILE_API_ENDPOINT_TokenEndpointId",
                        column: x => x.TokenEndpointId,
                        principalTable: "API_ENDPOINT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_API_AUTH_PROFILE_API_VERSION_ApiVersionId",
                        column: x => x.ApiVersionId,
                        principalTable: "API_VERSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_API_AUTH_PROFILE_ApiVersionId",
                table: "API_AUTH_PROFILE",
                column: "ApiVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_API_AUTH_PROFILE_TokenEndpointId",
                table: "API_AUTH_PROFILE",
                column: "TokenEndpointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "API_AUTH_PROFILE");

            migrationBuilder.DropColumn(
                name: "AuthenticationApplied",
                table: "TEST_EXECUTION");

            migrationBuilder.DropColumn(
                name: "AuthenticationProfileId",
                table: "TEST_EXECUTION");

            migrationBuilder.DropColumn(
                name: "AuthenticationTokenExpiresAtUtc",
                table: "TEST_EXECUTION");

            migrationBuilder.DropColumn(
                name: "AuthenticationTokenFromCache",
                table: "TEST_EXECUTION");

            migrationBuilder.DropColumn(
                name: "AuthenticationMode",
                table: "API_ENDPOINT");
        }
    }
}
