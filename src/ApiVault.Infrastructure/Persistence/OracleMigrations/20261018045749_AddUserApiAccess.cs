using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations
{
    /// <inheritdoc />
    public partial class AddUserApiAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USER_API_ACCESS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UserId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiVersionId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiEndpointId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiEnvironmentId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CanRead = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CanCreate = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CanEdit = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_API_ACCESS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USER_API_ACCESS_API_ASSET_ApiAssetId",
                        column: x => x.ApiAssetId,
                        principalTable: "API_ASSET",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_API_ACCESS_API_ENDPOINT_ApiEndpointId",
                        column: x => x.ApiEndpointId,
                        principalTable: "API_ENDPOINT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_API_ACCESS_API_ENVIRONMENT_ApiEnvironmentId",
                        column: x => x.ApiEnvironmentId,
                        principalTable: "API_ENVIRONMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_API_ACCESS_API_VERSION_ApiVersionId",
                        column: x => x.ApiVersionId,
                        principalTable: "API_VERSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_API_ACCESS_APP_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "APP_USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_API_ACCESS_PROJECT_REGISTRY_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PROJECT_REGISTRY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_ApiAssetId",
                table: "USER_API_ACCESS",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_ApiEndpointId",
                table: "USER_API_ACCESS",
                column: "ApiEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_ApiEnvironmentId",
                table: "USER_API_ACCESS",
                column: "ApiEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_ApiVersionId",
                table: "USER_API_ACCESS",
                column: "ApiVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_ApplicationId",
                table: "USER_API_ACCESS",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_UserId_ApiEndpointId_ApiEnvironmentId",
                table: "USER_API_ACCESS",
                columns: new[] { "UserId", "ApiEndpointId", "ApiEnvironmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_UserId_ApplicationId",
                table: "USER_API_ACCESS",
                columns: new[] { "UserId", "ApplicationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_API_ACCESS");
        }
    }
}
