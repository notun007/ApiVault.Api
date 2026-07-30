using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApiProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "API_PROJECT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_API_PROJECT", x => x.Id);
                });

            migrationBuilder.CreateIndex("IX_API_PROJECT_Code", "API_PROJECT", "Code", unique: true);
            migrationBuilder.CreateIndex("IX_API_PROJECT_Name", "API_PROJECT", "Name", unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO [API_PROJECT]
                    ([Id], [Code], [Name], [Description], [IsActive],
                     [CreatedAtUtc], [CreatedBy], [UpdatedAtUtc], [UpdatedBy])
                SELECT NEWID(),
                    LEFT('LEGACY-' + CONVERT(varchar(64), HASHBYTES('SHA2_256', [ApiProjectName]), 2), 100),
                    [ApiProjectName], NULL, CAST(1 AS bit), SYSUTCDATETIME(),
                    'migration:AddApiProject', NULL, NULL
                FROM [API_ASSET]
                GROUP BY [ApiProjectName];");

            migrationBuilder.AddColumn<Guid>("ApiProjectId", "API_ASSET", "uniqueidentifier", nullable: true);

            migrationBuilder.Sql(@"
                UPDATE asset
                SET [ApiProjectId] = project.[Id]
                FROM [API_ASSET] asset
                INNER JOIN [API_PROJECT] project ON project.[Name] = asset.[ApiProjectName];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM [API_ASSET] WHERE [ApiProjectId] IS NULL)
                    THROW 51000, 'ApiProject migration could not map every existing API asset to a project.', 1;");

            migrationBuilder.DropIndex("IX_API_ASSET_Name_ApiProjectName", "API_ASSET");
            migrationBuilder.DropColumn("ApiProjectName", "API_ASSET");
            migrationBuilder.AlterColumn<Guid>("ApiProjectId", "API_ASSET", "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);

            migrationBuilder.CreateIndex("IX_API_ASSET_ApiProjectId", "API_ASSET", "ApiProjectId");
            migrationBuilder.CreateIndex("IX_API_ASSET_Name_ApiProjectId", "API_ASSET", new[] { "Name", "ApiProjectId" }, unique: true);
            migrationBuilder.AddForeignKey(
                name: "FK_API_ASSET_API_PROJECT_ApiProjectId",
                table: "API_ASSET",
                column: "ApiProjectId",
                principalTable: "API_PROJECT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_API_ASSET_API_PROJECT_ApiProjectId", "API_ASSET");
            migrationBuilder.DropIndex("IX_API_ASSET_ApiProjectId", "API_ASSET");
            migrationBuilder.DropIndex("IX_API_ASSET_Name_ApiProjectId", "API_ASSET");

            migrationBuilder.AddColumn<string>("ApiProjectName", "API_ASSET", "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.Sql(@"
                UPDATE asset
                SET [ApiProjectName] = project.[Name]
                FROM [API_ASSET] asset
                INNER JOIN [API_PROJECT] project ON project.[Id] = asset.[ApiProjectId];");
            migrationBuilder.AlterColumn<string>("ApiProjectName", "API_ASSET", "nvarchar(200)", maxLength: 200, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(200)", oldMaxLength: 200, oldNullable: true);
            migrationBuilder.DropColumn("ApiProjectId", "API_ASSET");
            migrationBuilder.CreateIndex("IX_API_ASSET_Name_ApiProjectName", "API_ASSET", new[] { "Name", "ApiProjectName" }, unique: true);
            migrationBuilder.DropTable("API_PROJECT");
        }
    }
}
