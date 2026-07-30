using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APP_USER",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_USER", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AUDIT_LOG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_LOG", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BUSINESS_AREA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BUSINESS_AREA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DEV_TEAM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEV_TEAM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "API_ASSET",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OwnershipType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatorEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalReferenceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BusinessAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DevelopmentTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_API_ASSET", x => x.Id);
                    table.ForeignKey(
                        name: "FK_API_ASSET_BUSINESS_AREA_BusinessAreaId",
                        column: x => x.BusinessAreaId,
                        principalTable: "BUSINESS_AREA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_API_ASSET_DEV_TEAM_DevelopmentTeamId",
                        column: x => x.DevelopmentTeamId,
                        principalTable: "DEV_TEAM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT_REGISTRY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Criticality = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BusinessAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_REGISTRY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PROJECT_REGISTRY_BUSINESS_AREA_BusinessAreaId",
                        column: x => x.BusinessAreaId,
                        principalTable: "BUSINESS_AREA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROJECT_REGISTRY_DEV_TEAM_OwnerTeamId",
                        column: x => x.OwnerTeamId,
                        principalTable: "DEV_TEAM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "API_VERSION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReleaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LifecycleStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReleaseDateUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    DeprecatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    ChangeLog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthenticationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AuthenticationInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthenticationConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxRequestBytes = table.Column<int>(type: "int", nullable: false),
                    MaxResponseBytes = table.Column<int>(type: "int", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_API_VERSION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_API_VERSION_API_ASSET_ApiAssetId",
                        column: x => x.ApiAssetId,
                        principalTable: "API_ASSET",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "API_ENDPOINT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RequestHeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QueryParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PathParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestPayloadSample = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseHeadersSampleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseBodySample = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuccessStatusCodesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoapAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_API_ENDPOINT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_API_ENDPOINT_API_VERSION_ApiVersionId",
                        column: x => x.ApiVersionId,
                        principalTable: "API_VERSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "API_ENVIRONMENT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_API_ENVIRONMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_API_ENVIRONMENT_API_VERSION_ApiVersionId",
                        column: x => x.ApiVersionId,
                        principalTable: "API_VERSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT_API_VER",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_API_VER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PROJECT_API_VER_API_VERSION_ApiVersionId",
                        column: x => x.ApiVersionId,
                        principalTable: "API_VERSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROJECT_API_VER_PROJECT_REGISTRY_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "PROJECT_REGISTRY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ENV_SECRET",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiEnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EncryptedValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENV_SECRET", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ENV_SECRET_API_ENVIRONMENT_ApiEnvironmentId",
                        column: x => x.ApiEnvironmentId,
                        principalTable: "API_ENVIRONMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TEST_EXECUTION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiEndpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiEnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    RequestUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequestHeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ResponseHeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TEST_EXECUTION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TEST_EXECUTION_API_ENDPOINT_ApiEndpointId",
                        column: x => x.ApiEndpointId,
                        principalTable: "API_ENDPOINT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TEST_EXECUTION_API_ENVIRONMENT_ApiEnvironmentId",
                        column: x => x.ApiEnvironmentId,
                        principalTable: "API_ENVIRONMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_API_ASSET_BusinessAreaId",
                table: "API_ASSET",
                column: "BusinessAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_API_ASSET_DevelopmentTeamId",
                table: "API_ASSET",
                column: "DevelopmentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_API_ASSET_Name_ApiProjectName",
                table: "API_ASSET",
                columns: new[] { "Name", "ApiProjectName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_API_ENDPOINT_ApiVersionId_RelativePath_HttpMethod",
                table: "API_ENDPOINT",
                columns: new[] { "ApiVersionId", "RelativePath", "HttpMethod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_API_ENVIRONMENT_ApiVersionId_EnvironmentType",
                table: "API_ENVIRONMENT",
                columns: new[] { "ApiVersionId", "EnvironmentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_API_VERSION_ApiAssetId_Version",
                table: "API_VERSION",
                columns: new[] { "ApiAssetId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_APP_USER_Username",
                table: "APP_USER",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOG_EntityType_EntityId",
                table: "AUDIT_LOG",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOG_OccurredAtUtc",
                table: "AUDIT_LOG",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BUSINESS_AREA_Code",
                table: "BUSINESS_AREA",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DEV_TEAM_Code",
                table: "DEV_TEAM",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ENV_SECRET_ApiEnvironmentId_Name",
                table: "ENV_SECRET",
                columns: new[] { "ApiEnvironmentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_API_VER_ApiVersionId",
                table: "PROJECT_API_VER",
                column: "ApiVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_API_VER_ProjectId_ApiVersionId",
                table: "PROJECT_API_VER",
                columns: new[] { "ProjectId", "ApiVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_REGISTRY_BusinessAreaId",
                table: "PROJECT_REGISTRY",
                column: "BusinessAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_REGISTRY_Code",
                table: "PROJECT_REGISTRY",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_REGISTRY_OwnerTeamId",
                table: "PROJECT_REGISTRY",
                column: "OwnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TEST_EXECUTION_ApiEndpointId",
                table: "TEST_EXECUTION",
                column: "ApiEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_TEST_EXECUTION_ApiEnvironmentId",
                table: "TEST_EXECUTION",
                column: "ApiEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TEST_EXECUTION_StartedAtUtc",
                table: "TEST_EXECUTION",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APP_USER");

            migrationBuilder.DropTable(
                name: "AUDIT_LOG");

            migrationBuilder.DropTable(
                name: "ENV_SECRET");

            migrationBuilder.DropTable(
                name: "PROJECT_API_VER");

            migrationBuilder.DropTable(
                name: "TEST_EXECUTION");

            migrationBuilder.DropTable(
                name: "PROJECT_REGISTRY");

            migrationBuilder.DropTable(
                name: "API_ENDPOINT");

            migrationBuilder.DropTable(
                name: "API_ENVIRONMENT");

            migrationBuilder.DropTable(
                name: "API_VERSION");

            migrationBuilder.DropTable(
                name: "API_ASSET");

            migrationBuilder.DropTable(
                name: "BUSINESS_AREA");

            migrationBuilder.DropTable(
                name: "DEV_TEAM");
        }
    }
}
