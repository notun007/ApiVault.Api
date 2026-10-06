using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiVault.Infrastructure.Persistence;

internal static class ConfigurationHelpers
{
    public static void ConfigureAudit<TEntity>(
        EntityTypeBuilder<TEntity> builder)
        where TEntity : ApiVault.Domain.Common.AuditableEntity
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("RAW(16)");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("TIMESTAMP(6)")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("TIMESTAMP(6)");

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(200);
    }
}

public sealed class BusinessAreaConfiguration
    : IEntityTypeConfiguration<BusinessArea>
{
    public void Configure(EntityTypeBuilder<BusinessArea> b)
    {
        b.ToTable("BUSINESS_AREA");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Description)
            .HasMaxLength(4000);

        b.HasIndex(x => x.Code)
            .IsUnique();
    }
}

public sealed class DevelopmentTeamConfiguration
    : IEntityTypeConfiguration<DevelopmentTeam>
{
    public void Configure(EntityTypeBuilder<DevelopmentTeam> b)
    {
        b.ToTable("DEV_TEAM");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.ContactEmail)
            .HasMaxLength(320);

        b.Property(x => x.Description)
            .HasMaxLength(4000);

        b.HasIndex(x => x.Code)
            .IsUnique();
    }
}

public sealed class ApiAssetConfiguration
    : IEntityTypeConfiguration<ApiAsset>
{
    public void Configure(EntityTypeBuilder<ApiAsset> b)
    {
        b.ToTable("API_ASSET");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.PublishingApplicationId).HasColumnType("RAW(16)").IsRequired();

        b.Property(x => x.Description)
            .HasMaxLength(4000);

        b.Property(x => x.OwnershipType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.Protocol)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.CreatorName)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.CreatorEmail)
            .HasMaxLength(320);

        b.Property(x => x.VendorName)
            .HasMaxLength(200);

        b.Property(x => x.ExternalReferenceUrl)
            .HasMaxLength(1000);

        b.Property(x => x.BusinessAreaId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.DevelopmentTeamId)
            .HasColumnType("RAW(16)");

        b.HasIndex(x => new { x.Name, x.PublishingApplicationId })
            .IsUnique();

        b.HasOne(x => x.PublishingApplication)
            .WithMany(x => x.PublishedApis)
            .HasForeignKey(x => x.PublishingApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.BusinessArea)
            .WithMany(x => x.Apis)
            .HasForeignKey(x => x.BusinessAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.DevelopmentTeam)
            .WithMany(x => x.Apis)
            .HasForeignKey(x => x.DevelopmentTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ApiVersionConfiguration
    : IEntityTypeConfiguration<ApiVersion>
{
    public void Configure(EntityTypeBuilder<ApiVersion> b)
    {
        b.ToTable("API_VERSION");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.ApiAssetId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.Version)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.ReleaseName)
            .HasMaxLength(200);

        b.Property(x => x.LifecycleStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.ReleaseDateUtc)
            .HasColumnType("TIMESTAMP(6)");

        b.Property(x => x.DeprecatedAtUtc)
            .HasColumnType("TIMESTAMP(6)");

        b.Property(x => x.RetiredAtUtc)
            .HasColumnType("TIMESTAMP(6)");

        b.Property(x => x.ChangeLog)
            .HasColumnType("NCLOB");

        b.Property(x => x.AuthenticationType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.AuthenticationInstructions)
            .HasColumnType("NCLOB");

        b.Property(x => x.AuthenticationConfigJson)
            .HasColumnType("NCLOB");

        b.Property(x => x.IsCurrent)
            .HasColumnType("NUMBER(1)");

        b.HasIndex(x => new { x.ApiAssetId, x.Version })
            .IsUnique();

        b.HasOne(x => x.ApiAsset)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ApiAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ApiEndpointConfiguration
    : IEntityTypeConfiguration<ApiEndpoint>
{
    public void Configure(EntityTypeBuilder<ApiEndpoint> b)
    {
        b.ToTable("API_ENDPOINT");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.ApiVersionId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.RelativePath)
            .HasMaxLength(1000)
            .IsRequired();

        b.Property(x => x.HttpMethod)
            .HasMaxLength(20)
            .IsRequired();

        b.Property(x => x.Description)
            .HasMaxLength(4000);

        b.Property(x => x.RequestHeadersJson)
            .HasColumnType("NCLOB");

        b.Property(x => x.QueryParametersJson)
            .HasColumnType("NCLOB");

        b.Property(x => x.PathParametersJson)
            .HasColumnType("NCLOB");

        b.Property(x => x.RequestPayloadSample)
            .HasColumnType("NCLOB");

        b.Property(x => x.ResponseHeadersSampleJson)
            .HasColumnType("NCLOB");

        b.Property(x => x.ResponseBodySample)
            .HasColumnType("NCLOB");

        b.Property(x => x.SuccessStatusCodesJson)
            .HasColumnType("NCLOB");

        b.Property(x => x.SoapAction)
            .HasMaxLength(1000);

        b.HasIndex(x => new
        {
            x.ApiVersionId,
            x.RelativePath,
            x.HttpMethod
        }).IsUnique();

        b.HasOne(x => x.ApiVersion)
            .WithMany(x => x.Endpoints)
            .HasForeignKey(x => x.ApiVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ApiEnvironmentConfiguration
    : IEntityTypeConfiguration<ApiEnvironment>
{
    public void Configure(EntityTypeBuilder<ApiEnvironment> b)
    {
        b.ToTable("API_ENVIRONMENT");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.ApiVersionId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.EnvironmentType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.BaseUrl)
            .HasMaxLength(1000)
            .IsRequired();

        b.Property(x => x.IsEnabled)
            .HasColumnType("NUMBER(1)");

        b.Property(x => x.Notes)
            .HasMaxLength(4000);

        b.HasIndex(x => new
        {
            x.ApiVersionId,
            x.EnvironmentType
        }).IsUnique();

        b.HasOne(x => x.ApiVersion)
            .WithMany(x => x.Environments)
            .HasForeignKey(x => x.ApiVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EnvironmentSecretConfiguration
    : IEntityTypeConfiguration<EnvironmentSecret>
{
    public void Configure(EntityTypeBuilder<EnvironmentSecret> b)
    {
        b.ToTable("ENV_SECRET");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.ApiEnvironmentId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.EncryptedValue)
            .HasColumnType("NCLOB")
            .IsRequired();

        b.HasIndex(x => new
        {
            x.ApiEnvironmentId,
            x.Name
        }).IsUnique();

        b.HasOne(x => x.ApiEnvironment)
            .WithMany(x => x.Secrets)
            .HasForeignKey(x => x.ApiEnvironmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectConfiguration
    : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("PROJECT_REGISTRY");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Description)
            .HasMaxLength(4000);

        b.Property(x => x.Criticality)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.BusinessAreaId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.OwnerTeamId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.OwnershipType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.VendorId)
            .HasColumnType("RAW(16)");

        b.HasIndex(x => x.Code)
            .IsUnique();

        b.HasOne(x => x.BusinessArea)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.BusinessAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.OwnerTeam)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.OwnerTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Vendor)
            .WithMany(x => x.Systems)
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> b)
    {
        b.ToTable("VENDOR");
        ConfigurationHelpers.ConfigureAudit(b);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.ContactPerson).HasMaxLength(200);
        b.Property(x => x.SupportEmail).HasMaxLength(320);
        b.Property(x => x.SupportPhone).HasMaxLength(100);
        b.Property(x => x.WebsiteUrl).HasMaxLength(1000);
        b.Property(x => x.IsActive).HasColumnType("NUMBER(1)");
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class ProjectApiVersionConfiguration
    : IEntityTypeConfiguration<ProjectApiVersion>
{
    public void Configure(EntityTypeBuilder<ProjectApiVersion> b)
    {
        b.ToTable("PROJECT_API_VER");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.ProjectId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.ApiVersionId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.Purpose)
            .HasMaxLength(2000);

        b.Property(x => x.IsRequired)
            .HasColumnType("NUMBER(1)");

        b.HasIndex(x => new
        {
            x.ProjectId,
            x.ApiVersionId
        }).IsUnique();

        b.HasOne(x => x.Project)
            .WithMany(x => x.ApiLinks)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ApiVersion)
            .WithMany(x => x.ProjectLinks)
            .HasForeignKey(x => x.ApiVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AppUserConfiguration
    : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("APP_USER");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.Username)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Email)
            .HasMaxLength(320);

        b.Property(x => x.PasswordHash)
            .HasMaxLength(1000)
            .IsRequired();

        b.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.IsActive)
            .HasColumnType("NUMBER(1)");

        b.Property(x => x.LastLoginAtUtc)
            .HasColumnType("TIMESTAMP(6)");

        b.HasIndex(x => x.Username)
            .IsUnique();
    }
}

public sealed class TestExecutionConfiguration
    : IEntityTypeConfiguration<TestExecution>
{
    public void Configure(EntityTypeBuilder<TestExecution> b)
    {
        b.ToTable("TEST_EXECUTION");

        ConfigurationHelpers.ConfigureAudit(b);

        b.Property(x => x.ApiEndpointId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.ApiEnvironmentId)
            .HasColumnType("RAW(16)");

        b.Property(x => x.StartedAtUtc)
            .HasColumnType("TIMESTAMP(6)");

        b.Property(x => x.IsSuccess)
            .HasColumnType("NUMBER(1)");

        b.Property(x => x.RequestUrl)
            .HasMaxLength(2000)
            .IsRequired();

        b.Property(x => x.RequestHeadersJson)
            .HasColumnType("NCLOB");

        b.Property(x => x.RequestBody)
            .HasColumnType("NCLOB");

        b.Property(x => x.ResponseHeadersJson)
            .HasColumnType("NCLOB");

        b.Property(x => x.ResponseBody)
            .HasColumnType("NCLOB");

        b.Property(x => x.ErrorMessage)
            .HasColumnType("NCLOB");

        b.HasIndex(x => x.StartedAtUtc);

        b.HasOne(x => x.ApiEndpoint)
            .WithMany(x => x.TestExecutions)
            .HasForeignKey(x => x.ApiEndpointId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ApiEnvironment)
            .WithMany(x => x.TestExecutions)
            .HasForeignKey(x => x.ApiEnvironmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AUDIT_LOG");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnType("RAW(16)");

        b.Property(x => x.OccurredAtUtc)
            .HasColumnType("TIMESTAMP(6)")
            .IsRequired();

        b.Property(x => x.UserName)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.IpAddress)
            .HasMaxLength(100);

        b.Property(x => x.CorrelationId)
            .HasMaxLength(200);

        b.Property(x => x.Action)
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.EntityType)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.ChangesJson)
            .HasColumnType("NCLOB");

        b.HasIndex(x => x.OccurredAtUtc);

        b.HasIndex(x => new
        {
            x.EntityType,
            x.EntityId
        });
    }
}






//using ApiVault.Domain.Entities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;

//namespace ApiVault.Infrastructure.Persistence;

//internal static class ConfigurationHelpers
//{
//    public static void ConfigureAudit<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : ApiVault.Domain.Common.AuditableEntity
//    {
//        builder.HasKey(x => x.Id);
//        builder.Property(x => x.Id).HasColumnType("RAW(16)");
//        builder.Property(x => x.CreatedAtUtc).HasColumnType("TIMESTAMP(6)").IsRequired();
//        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
//        builder.Property(x => x.UpdatedAtUtc).HasColumnType("TIMESTAMP(6)");
//        builder.Property(x => x.UpdatedBy).HasMaxLength(200);
//    }
//}

//public sealed class BusinessAreaConfiguration : IEntityTypeConfiguration<BusinessArea>
//{
//    public void Configure(EntityTypeBuilder<BusinessArea> b)
//    {
//        b.ToTable("BUSINESS_AREA"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
//        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
//        b.Property(x => x.Description).HasMaxLength(4000);
//        b.HasIndex(x => x.Code).IsUnique();
//    }
//}

//public sealed class DevelopmentTeamConfiguration : IEntityTypeConfiguration<DevelopmentTeam>
//{
//    public void Configure(EntityTypeBuilder<DevelopmentTeam> b)
//    {
//        b.ToTable("DEV_TEAM"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
//        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
//        b.Property(x => x.ContactEmail).HasMaxLength(320);
//        b.Property(x => x.Description).HasMaxLength(4000);
//        b.HasIndex(x => x.Code).IsUnique();
//    }
//}

//public sealed class ApiProjectConfiguration : IEntityTypeConfiguration<ApiProject>
//{
//    public void Configure(EntityTypeBuilder<ApiProject> b)
//    {
//        b.ToTable("API_PROJECT");
//        ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
//        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
//        b.Property(x => x.Description).HasMaxLength(4000);
//        b.Property(x => x.IsActive).IsRequired();
//        b.HasIndex(x => x.Code).IsUnique();
//        b.HasIndex(x => x.Name).IsUnique();
//    }
//}

//public sealed class ApiAssetConfiguration : IEntityTypeConfiguration<ApiAsset>
//{
//    public void Configure(EntityTypeBuilder<ApiAsset> b)
//    {
//        b.ToTable("API_ASSET"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
//        b.Property(x => x.ApiProjectName).HasMaxLength(200).IsRequired();
//        b.Property(x => x.Description).HasMaxLength(4000);
//        b.Property(x => x.OwnershipType).HasConversion<string>().HasMaxLength(30).IsRequired();
//        b.Property(x => x.Protocol).HasConversion<string>().HasMaxLength(30).IsRequired();
//        b.Property(x => x.CreatorName).HasMaxLength(200).IsRequired();
//        b.Property(x => x.CreatorEmail).HasMaxLength(320);
//        b.Property(x => x.VendorName).HasMaxLength(200);
//        b.Property(x => x.ExternalReferenceUrl).HasMaxLength(1000);
//        b.Property(x => x.BusinessAreaId).HasColumnType("RAW(16)");
//        b.Property(x => x.DevelopmentTeamId).HasColumnType("RAW(16)");
//        b.HasIndex(x => new { x.Name, x.ApiProjectName }).IsUnique();
//        b.HasOne(x => x.BusinessArea).WithMany(x => x.Apis).HasForeignKey(x => x.BusinessAreaId).OnDelete(DeleteBehavior.Restrict);
//        b.HasOne(x => x.DevelopmentTeam).WithMany(x => x.Apis).HasForeignKey(x => x.DevelopmentTeamId).OnDelete(DeleteBehavior.Restrict);
//    }
//}

//public sealed class ApiVersionConfiguration : IEntityTypeConfiguration<ApiVersion>
//{
//    public void Configure(EntityTypeBuilder<ApiVersion> b)
//    {
//        b.ToTable("API_VERSION"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.ApiAssetId).HasColumnType("RAW(16)");
//        b.Property(x => x.Version).HasMaxLength(50).IsRequired();
//        b.Property(x => x.ReleaseName).HasMaxLength(200);
//        b.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
//        b.Property(x => x.ReleaseDateUtc).HasColumnType("TIMESTAMP(6)");
//        b.Property(x => x.DeprecatedAtUtc).HasColumnType("TIMESTAMP(6)");
//        b.Property(x => x.RetiredAtUtc).HasColumnType("TIMESTAMP(6)");
//        b.Property(x => x.ChangeLog).HasColumnType("CLOB");
//        b.Property(x => x.AuthenticationType).HasConversion<string>().HasMaxLength(30).IsRequired();
//        b.Property(x => x.AuthenticationInstructions).HasColumnType("CLOB");
//        b.Property(x => x.AuthenticationConfigJson).HasColumnType("CLOB");
//        b.Property(x => x.IsCurrent).HasColumnType("NUMBER(1)");
//        b.HasIndex(x => new { x.ApiAssetId, x.Version }).IsUnique();
//        b.HasOne(x => x.ApiAsset).WithMany(x => x.Versions).HasForeignKey(x => x.ApiAssetId).OnDelete(DeleteBehavior.Restrict);
//    }
//}

//public sealed class ApiEndpointConfiguration : IEntityTypeConfiguration<ApiEndpoint>
//{
//    public void Configure(EntityTypeBuilder<ApiEndpoint> b)
//    {
//        b.ToTable("API_ENDPOINT"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.ApiVersionId).HasColumnType("RAW(16)");
//        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
//        b.Property(x => x.RelativePath).HasMaxLength(1000).IsRequired();
//        b.Property(x => x.HttpMethod).HasMaxLength(20).IsRequired();
//        b.Property(x => x.Description).HasMaxLength(4000);
//        b.Property(x => x.RequestHeadersJson).HasColumnType("CLOB");
//        b.Property(x => x.QueryParametersJson).HasColumnType("CLOB");
//        b.Property(x => x.PathParametersJson).HasColumnType("CLOB");
//        b.Property(x => x.RequestPayloadSample).HasColumnType("CLOB");
//        b.Property(x => x.ResponseHeadersSampleJson).HasColumnType("CLOB");
//        b.Property(x => x.ResponseBodySample).HasColumnType("CLOB");
//        b.Property(x => x.SuccessStatusCodesJson).HasColumnType("CLOB");
//        b.Property(x => x.SoapAction).HasMaxLength(1000);
//        b.HasIndex(x => new { x.ApiVersionId, x.RelativePath, x.HttpMethod }).IsUnique();
//        b.HasOne(x => x.ApiVersion).WithMany(x => x.Endpoints).HasForeignKey(x => x.ApiVersionId).OnDelete(DeleteBehavior.Restrict);
//    }
//}

//public sealed class ApiEnvironmentConfiguration : IEntityTypeConfiguration<ApiEnvironment>
//{
//    public void Configure(EntityTypeBuilder<ApiEnvironment> b)
//    {
//        b.ToTable("API_ENVIRONMENT"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.ApiVersionId).HasColumnType("RAW(16)");
//        b.Property(x => x.EnvironmentType).HasConversion<string>().HasMaxLength(30).IsRequired();
//        b.Property(x => x.BaseUrl).HasMaxLength(1000).IsRequired();
//        b.Property(x => x.IsEnabled).HasColumnType("NUMBER(1)");
//        b.Property(x => x.Notes).HasMaxLength(4000);
//        b.HasIndex(x => new { x.ApiVersionId, x.EnvironmentType }).IsUnique();
//        b.HasOne(x => x.ApiVersion).WithMany(x => x.Environments).HasForeignKey(x => x.ApiVersionId).OnDelete(DeleteBehavior.Restrict);
//    }
//}

//public sealed class EnvironmentSecretConfiguration : IEntityTypeConfiguration<EnvironmentSecret>
//{
//    public void Configure(EntityTypeBuilder<EnvironmentSecret> b)
//    {
//        b.ToTable("ENV_SECRET"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.ApiEnvironmentId).HasColumnType("RAW(16)");
//        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
//        b.Property(x => x.EncryptedValue).HasColumnType("CLOB").IsRequired();
//        b.HasIndex(x => new { x.ApiEnvironmentId, x.Name }).IsUnique();
//        b.HasOne(x => x.ApiEnvironment).WithMany(x => x.Secrets).HasForeignKey(x => x.ApiEnvironmentId).OnDelete(DeleteBehavior.Restrict);
//    }
//}

//public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
//{
//    public void Configure(EntityTypeBuilder<Project> b)
//    {
//        b.ToTable("PROJECT_REGISTRY"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
//        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
//        b.Property(x => x.Description).HasMaxLength(4000);
//        b.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(30).IsRequired();
//        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
//        b.Property(x => x.BusinessAreaId).HasColumnType("RAW(16)");
//        b.Property(x => x.OwnerTeamId).HasColumnType("RAW(16)");
//        b.HasIndex(x => x.Code).IsUnique();
//        b.HasOne(x => x.BusinessArea).WithMany(x => x.Projects).HasForeignKey(x => x.BusinessAreaId).OnDelete(DeleteBehavior.Restrict);
//        b.HasOne(x => x.OwnerTeam).WithMany(x => x.Projects).HasForeignKey(x => x.OwnerTeamId).OnDelete(DeleteBehavior.Restrict);
//    }
//}

//public sealed class ProjectApiVersionConfiguration : IEntityTypeConfiguration<ProjectApiVersion>
//{
//    public void Configure(EntityTypeBuilder<ProjectApiVersion> b)
//    {
//        b.ToTable("PROJECT_API_VER"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.ProjectId).HasColumnType("RAW(16)");
//        b.Property(x => x.ApiVersionId).HasColumnType("RAW(16)");
//        b.Property(x => x.Purpose).HasMaxLength(2000);
//        b.Property(x => x.IsRequired).HasColumnType("NUMBER(1)");
//        b.HasIndex(x => new { x.ProjectId, x.ApiVersionId }).IsUnique();
//        b.HasOne(x => x.Project).WithMany(x => x.ApiLinks).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
//        b.HasOne(x => x.ApiVersion).WithMany(x => x.ProjectLinks).HasForeignKey(x => x.ApiVersionId).OnDelete(DeleteBehavior.Restrict);
//    }
//}

//public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
//{
//    public void Configure(EntityTypeBuilder<AppUser> b)
//    {
//        b.ToTable("APP_USER"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.Username).HasMaxLength(100).IsRequired();
//        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
//        b.Property(x => x.Email).HasMaxLength(320);
//        b.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
//        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
//        b.Property(x => x.IsActive).HasColumnType("NUMBER(1)");
//        b.Property(x => x.LastLoginAtUtc).HasColumnType("TIMESTAMP(6)");
//        b.HasIndex(x => x.Username).IsUnique();
//    }
//}

//public sealed class TestExecutionConfiguration : IEntityTypeConfiguration<TestExecution>
//{
//    public void Configure(EntityTypeBuilder<TestExecution> b)
//    {
//        b.ToTable("TEST_EXECUTION"); ConfigurationHelpers.ConfigureAudit(b);
//        b.Property(x => x.ApiEndpointId).HasColumnType("RAW(16)");
//        b.Property(x => x.ApiEnvironmentId).HasColumnType("RAW(16)");
//        b.Property(x => x.StartedAtUtc).HasColumnType("TIMESTAMP(6)");
//        b.Property(x => x.IsSuccess).HasColumnType("NUMBER(1)");
//        b.Property(x => x.RequestUrl).HasMaxLength(2000).IsRequired();
//        b.Property(x => x.RequestHeadersJson).HasColumnType("CLOB");
//        b.Property(x => x.RequestBody).HasColumnType("CLOB");
//        b.Property(x => x.ResponseHeadersJson).HasColumnType("CLOB");
//        b.Property(x => x.ResponseBody).HasColumnType("CLOB");
//        b.Property(x => x.ErrorMessage).HasColumnType("CLOB");
//        b.HasIndex(x => x.StartedAtUtc);
//        b.HasOne(x => x.ApiEndpoint).WithMany(x => x.TestExecutions).HasForeignKey(x => x.ApiEndpointId).OnDelete(DeleteBehavior.Restrict);
//        b.HasOne(x => x.ApiEnvironment).WithMany(x => x.TestExecutions).HasForeignKey(x => x.ApiEnvironmentId).OnDelete(DeleteBehavior.Restrict);
//    }
//}

//public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
//{
//    public void Configure(EntityTypeBuilder<AuditLog> b)
//    {
//        b.ToTable("AUDIT_LOG");
//        b.HasKey(x => x.Id);
//        b.Property(x => x.Id).HasColumnType("RAW(16)");
//        b.Property(x => x.OccurredAtUtc).HasColumnType("TIMESTAMP(6)").IsRequired();
//        b.Property(x => x.UserName).HasMaxLength(200).IsRequired();
//        b.Property(x => x.IpAddress).HasMaxLength(100);
//        b.Property(x => x.CorrelationId).HasMaxLength(200);
//        b.Property(x => x.Action).HasMaxLength(30).IsRequired();
//        b.Property(x => x.EntityType).HasMaxLength(200).IsRequired();
//        b.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
//        b.Property(x => x.ChangesJson).HasColumnType("CLOB");
//        b.HasIndex(x => x.OccurredAtUtc);
//        b.HasIndex(x => new { x.EntityType, x.EntityId });
//    }
//}
