using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations
{
    /// <inheritdoc />
    public partial class NormalizeUserApiAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USER_API_ACCESS_API_ASSET_ApiAssetId",
                table: "USER_API_ACCESS");

            migrationBuilder.DropForeignKey(
                name: "FK_USER_API_ACCESS_API_VERSION_ApiVersionId",
                table: "USER_API_ACCESS");

            migrationBuilder.DropIndex(
                name: "IX_USER_API_ACCESS_ApiAssetId",
                table: "USER_API_ACCESS");

            migrationBuilder.DropIndex(
                name: "IX_USER_API_ACCESS_ApiVersionId",
                table: "USER_API_ACCESS");

            migrationBuilder.DropIndex(
                name: "IX_USER_API_ACCESS_UserId_ApiEndpointId_ApiEnvironmentId",
                table: "USER_API_ACCESS");

            migrationBuilder.RenameColumn(
                name: "CanRead",
                table: "USER_API_ACCESS",
                newName: "IsActive");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFromUtc",
                table: "USER_API_ACCESS",
                type: "TIMESTAMP(7)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidToUtc",
                table: "USER_API_ACCESS",
                type: "TIMESTAMP(7)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "USER_API_ACCESS_ACTION",
                columns: table => new
                {
                    GrantId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ActionCode = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_API_ACCESS_ACTION", x => new { x.GrantId, x.ActionCode });
                    table.ForeignKey(
                        name: "FK_USER_API_ACCESS_ACTION_USER_API_ACCESS_GrantId",
                        column: x => x.GrantId,
                        principalTable: "USER_API_ACCESS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_API_ACCESS_AUDIT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    GrantId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    UserId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ActionType = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    OldValueJson = table.Column<string>(type: "NCLOB", nullable: true),
                    NewValueJson = table.Column<string>(type: "NCLOB", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_API_ACCESS_AUDIT", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO \"USER_API_ACCESS_ACTION\" (\"GrantId\", \"ActionCode\") SELECT \"Id\", 'READ' FROM \"USER_API_ACCESS\"");
            migrationBuilder.Sql("INSERT INTO \"USER_API_ACCESS_ACTION\" (\"GrantId\", \"ActionCode\") SELECT \"Id\", 'CREATE' FROM \"USER_API_ACCESS\" WHERE \"CanCreate\" = 1");
            migrationBuilder.Sql("INSERT INTO \"USER_API_ACCESS_ACTION\" (\"GrantId\", \"ActionCode\") SELECT \"Id\", 'EDIT' FROM \"USER_API_ACCESS\" WHERE \"CanEdit\" = 1");

            migrationBuilder.DropColumn(
                name: "ApiAssetId",
                table: "USER_API_ACCESS");

            migrationBuilder.DropColumn(
                name: "ApiVersionId",
                table: "USER_API_ACCESS");

            migrationBuilder.DropColumn(
                name: "CanCreate",
                table: "USER_API_ACCESS");

            migrationBuilder.DropColumn(
                name: "CanEdit",
                table: "USER_API_ACCESS");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_UserId_ApplicationId_ApiEndpointId_ApiEnvironmentId",
                table: "USER_API_ACCESS",
                columns: new[] { "UserId", "ApplicationId", "ApiEndpointId", "ApiEnvironmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_AUDIT_GrantId",
                table: "USER_API_ACCESS_AUDIT",
                column: "GrantId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_AUDIT_UserId_CreatedAtUtc",
                table: "USER_API_ACCESS_AUDIT",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_API_ACCESS_ACTION");

            migrationBuilder.DropTable(
                name: "USER_API_ACCESS_AUDIT");

            migrationBuilder.DropIndex(
                name: "IX_USER_API_ACCESS_UserId_ApplicationId_ApiEndpointId_ApiEnvironmentId",
                table: "USER_API_ACCESS");

            migrationBuilder.DropColumn(
                name: "ValidFromUtc",
                table: "USER_API_ACCESS");

            migrationBuilder.DropColumn(
                name: "ValidToUtc",
                table: "USER_API_ACCESS");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "USER_API_ACCESS",
                newName: "CanRead");

            migrationBuilder.AddColumn<Guid>(
                name: "ApiAssetId",
                table: "USER_API_ACCESS",
                type: "RAW(16)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ApiVersionId",
                table: "USER_API_ACCESS",
                type: "RAW(16)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "CanCreate",
                table: "USER_API_ACCESS",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanEdit",
                table: "USER_API_ACCESS",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_ApiAssetId",
                table: "USER_API_ACCESS",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_ApiVersionId",
                table: "USER_API_ACCESS",
                column: "ApiVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_API_ACCESS_UserId_ApiEndpointId_ApiEnvironmentId",
                table: "USER_API_ACCESS",
                columns: new[] { "UserId", "ApiEndpointId", "ApiEnvironmentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_USER_API_ACCESS_API_ASSET_ApiAssetId",
                table: "USER_API_ACCESS",
                column: "ApiAssetId",
                principalTable: "API_ASSET",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USER_API_ACCESS_API_VERSION_ApiVersionId",
                table: "USER_API_ACCESS",
                column: "ApiVersionId",
                principalTable: "API_VERSION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
