using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiVault.Infrastructure.Persistence.OracleMigrations
{
    /// <inheritdoc />
    public partial class OracleInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APP_USER",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Username = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(320)", maxLength: 320, nullable: true),
                    PasswordHash = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: false),
                    Role = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    LastLoginAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_USER", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AUDIT_LOG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    UserName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    Action = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    EntityType = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    ChangesJson = table.Column<string>(type: "NCLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_LOG", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BUSINESS_AREA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Code = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BUSINESS_AREA", x => x.Id);
                });

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEV_TEAM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SEC_PERMISSION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Code = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEC_PERMISSION", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SEC_ROLE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Code = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEC_ROLE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SEC_SCREEN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Code = table.Column<string>(type: "NVARCHAR2(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Route = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    Icon = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    ParentId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IsActive = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEC_SCREEN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SEC_SCREEN_SEC_SCREEN_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SEC_SCREEN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VENDOR",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Code = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: true),
                    ContactPerson = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    SupportEmail = table.Column<string>(type: "NVARCHAR2(320)", maxLength: 320, nullable: true),
                    SupportPhone = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VENDOR", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "APP_USER_ROLE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UserId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    RoleId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_USER_ROLE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_APP_USER_ROLE_APP_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "APP_USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_APP_USER_ROLE_SEC_ROLE_RoleId",
                        column: x => x.RoleId,
                        principalTable: "SEC_ROLE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ROLE_PERMISSION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    RoleId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ScreenId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    PermissionId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLE_PERMISSION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_SEC_PERMISSION_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "SEC_PERMISSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_SEC_ROLE_RoleId",
                        column: x => x.RoleId,
                        principalTable: "SEC_ROLE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_SEC_SCREEN_ScreenId",
                        column: x => x.ScreenId,
                        principalTable: "SEC_SCREEN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT_REGISTRY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Code = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: true),
                    Criticality = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    BusinessAreaId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    OwnerTeamId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    OwnershipType = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    VendorId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
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
                    table.ForeignKey(
                        name: "FK_PROJECT_REGISTRY_VENDOR_VendorId",
                        column: x => x.VendorId,
                        principalTable: "VENDOR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "API_ASSET",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    PublishingApplicationId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Description = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: true),
                    OwnershipType = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Protocol = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    CreatorName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    CreatorEmail = table.Column<string>(type: "NVARCHAR2(320)", maxLength: 320, nullable: true),
                    VendorName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ExternalReferenceUrl = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    BusinessAreaId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    DevelopmentTeamId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
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
                    table.ForeignKey(
                        name: "FK_API_ASSET_PROJECT_REGISTRY_PublishingApplicationId",
                        column: x => x.PublishingApplicationId,
                        principalTable: "PROJECT_REGISTRY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "API_VERSION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Version = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    ReleaseName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    LifecycleStatus = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    ReleaseDateUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    DeprecatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    ChangeLog = table.Column<string>(type: "NCLOB", nullable: true),
                    AuthenticationType = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    AuthenticationInstructions = table.Column<string>(type: "NCLOB", nullable: true),
                    AuthenticationConfigJson = table.Column<string>(type: "NCLOB", nullable: true),
                    MaxRequestBytes = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MaxResponseBytes = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IsCurrent = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
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
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiVersionId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    RelativePath = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: false),
                    HttpMethod = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: true),
                    RequestHeadersJson = table.Column<string>(type: "NCLOB", nullable: true),
                    QueryParametersJson = table.Column<string>(type: "NCLOB", nullable: true),
                    PathParametersJson = table.Column<string>(type: "NCLOB", nullable: true),
                    RequestPayloadSample = table.Column<string>(type: "NCLOB", nullable: true),
                    ResponseHeadersSampleJson = table.Column<string>(type: "NCLOB", nullable: true),
                    ResponseBodySample = table.Column<string>(type: "NCLOB", nullable: true),
                    SuccessStatusCodesJson = table.Column<string>(type: "NCLOB", nullable: true),
                    SoapAction = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
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
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiVersionId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    EnvironmentType = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    BaseUrl = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Notes = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
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
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiVersionId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Purpose = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
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
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiEnvironmentId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    EncryptedValue = table.Column<string>(type: "NCLOB", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
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
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiEndpointId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApiEnvironmentId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    IsSuccess = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    RequestUrl = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    RequestHeadersJson = table.Column<string>(type: "NCLOB", nullable: true),
                    RequestBody = table.Column<string>(type: "NCLOB", nullable: true),
                    RequestSizeBytes = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    ResponseHeadersJson = table.Column<string>(type: "NCLOB", nullable: true),
                    ResponseBody = table.Column<string>(type: "NCLOB", nullable: true),
                    ResponseSizeBytes = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "NCLOB", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true)
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
                name: "IX_API_ASSET_Name_PublishingApplicationId",
                table: "API_ASSET",
                columns: new[] { "Name", "PublishingApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_API_ASSET_PublishingApplicationId",
                table: "API_ASSET",
                column: "PublishingApplicationId");

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
                name: "IX_APP_USER_ROLE_RoleId",
                table: "APP_USER_ROLE",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_APP_USER_ROLE_UserId_RoleId",
                table: "APP_USER_ROLE",
                columns: new[] { "UserId", "RoleId" },
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
                name: "IX_PROJECT_REGISTRY_VendorId",
                table: "PROJECT_REGISTRY",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_PermissionId",
                table: "ROLE_PERMISSION",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_RoleId_ScreenId_PermissionId",
                table: "ROLE_PERMISSION",
                columns: new[] { "RoleId", "ScreenId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_ScreenId",
                table: "ROLE_PERMISSION",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_SEC_PERMISSION_Code",
                table: "SEC_PERMISSION",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SEC_ROLE_Code",
                table: "SEC_ROLE",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SEC_SCREEN_Code",
                table: "SEC_SCREEN",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SEC_SCREEN_ParentId",
                table: "SEC_SCREEN",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SEC_SCREEN_Route",
                table: "SEC_SCREEN",
                column: "Route",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_VENDOR_Code",
                table: "VENDOR",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VENDOR_Name",
                table: "VENDOR",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APP_USER_ROLE");

            migrationBuilder.DropTable(
                name: "AUDIT_LOG");

            migrationBuilder.DropTable(
                name: "ENV_SECRET");

            migrationBuilder.DropTable(
                name: "PROJECT_API_VER");

            migrationBuilder.DropTable(
                name: "ROLE_PERMISSION");

            migrationBuilder.DropTable(
                name: "TEST_EXECUTION");

            migrationBuilder.DropTable(
                name: "APP_USER");

            migrationBuilder.DropTable(
                name: "SEC_PERMISSION");

            migrationBuilder.DropTable(
                name: "SEC_ROLE");

            migrationBuilder.DropTable(
                name: "SEC_SCREEN");

            migrationBuilder.DropTable(
                name: "API_ENDPOINT");

            migrationBuilder.DropTable(
                name: "API_ENVIRONMENT");

            migrationBuilder.DropTable(
                name: "API_VERSION");

            migrationBuilder.DropTable(
                name: "API_ASSET");

            migrationBuilder.DropTable(
                name: "PROJECT_REGISTRY");

            migrationBuilder.DropTable(
                name: "BUSINESS_AREA");

            migrationBuilder.DropTable(
                name: "DEV_TEAM");

            migrationBuilder.DropTable(
                name: "VENDOR");
        }
    }
}
