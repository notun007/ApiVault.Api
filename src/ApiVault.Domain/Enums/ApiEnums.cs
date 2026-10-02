namespace ApiVault.Domain.Enums;

public enum ApiOwnershipType
{
    Internal = 1,
    ThirdParty = 2
}

public enum ApiProtocol
{
    Rest = 1,
    Soap = 2,
    WebService = 3
}

public enum ApiLifecycleStatus
{
    Draft = 1,
    Active = 2,
    Deprecated = 3,
    Retired = 4
}

public enum AuthenticationType
{
    None = 0,
    Bearer = 1,
    Basic = 2,
    ApiKey = 3,
    OAuth2 = 4,
    MutualTls = 5,
    Custom = 99
}

public enum DeploymentEnvironment
{
    Development = 1,
    Uat = 2,
    Production = 3,
    DisasterRecovery = 4,
    Sandbox = 5
}

public enum ProjectStatus
{
    Active = 1,
    Inactive = 2,
    Retired = 3
}

public enum ProjectCriticality
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum UserRole
{
    Admin = 1,
    ApiOwner = 2,
    Tester = 3,
    Viewer = 4,
    SuperAdmin = 5
}
