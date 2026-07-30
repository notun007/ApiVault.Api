namespace ApiVault.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "ApiVault";
    public string Audience { get; set; } = "ApiVault.Client";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class ExecutionSecurityOptions
{
    public const string SectionName = "ApiExecutionSecurity";
    public bool AllowHttp { get; set; }
    public bool AllowPrivateNetworks { get; set; }
    public string[] AllowedHosts { get; set; } = Array.Empty<string>();
    public string[] AdditionalSensitiveHeaders { get; set; } = Array.Empty<string>();
    public int AbsoluteMaxRequestBytes { get; set; } = 10_485_760;
    public int AbsoluteMaxResponseBytes { get; set; } = 20_971_520;
    public int AbsoluteMaxTimeoutSeconds { get; set; } = 120;
}

public sealed class DatabaseInitializationOptions
{
    public const string SectionName = "DatabaseInitialization";
    public bool Enabled { get; set; }
    public bool ApplyMigrations { get; set; }
    public bool SeedReferenceData { get; set; } = true;
    public bool SeedAdmin { get; set; }
    public string AdminUsername { get; set; } = "admin";
    public string AdminDisplayName { get; set; } = "ApiVault Administrator";
    public string? AdminEmail { get; set; }
    public string AdminPassword { get; set; } = string.Empty;
}
