# ApiVault

ApiVault is a banking-oriented API inventory, governance, dependency-tracing, and controlled test-execution backend.

This edition intentionally exposes only business REST endpoints and health checks; no generated API-reference or interactive documentation subsystem is included.

## Solution structure

```text
ApiVault.sln
src/
  ApiVault.Api             ASP.NET Core 10 controllers, JWT, health endpoint
  ApiVault.Application     Use cases, DTOs, validation, catalog/test services
  ApiVault.Domain          Entities and enums
  ApiVault.Infrastructure  Oracle EF Core, security, HTTP execution, auditing
```

## Implemented capabilities

- Internal and third-party API registration.
- REST, SOAP, and generic web-service classification.
- Unified system/application registry: a system can publish APIs, consume APIs, or do both.
- API creator, publishing system, owner development team, vendor company, and business area.
- Third-party vendor-company registry with support contacts and source-system linkage.
- Self-service password change and administrator/Super Administrator password reset.
- Multiple immutable version identities with editable release metadata.
- Draft, Active, Deprecated, and Retired lifecycle transitions.
- Endpoint method, relative URL, headers, query/path definitions, sample payload, sample response, SOAPAction, and success codes.
- Bearer, Basic, API Key, OAuth2, mutual TLS, and custom authentication instructions.
- Development, UAT, Production, DR, and Sandbox environment registration.
- Environment secrets encrypted at rest with ASP.NET Core Data Protection; secret values are never returned by the API.
- Project registry and linkage to an exact API version.
- Built-in controlled HTTP test execution.
- Test history with status, headers, body, duration, and request/response byte counts.
- Secret header and known secret-value redaction before history is stored.
- Request/response size ceilings and per-version timeout settings.
- SSRF controls for localhost, private networks, link-local destinations, multicast/reserved ranges, and cloud metadata.
- Explicit allowlisting for approved internal bank hosts.
- Redirects disabled during test execution.
- Audit logs for added, modified, and deleted tracked entities; password and secret fields are redacted.
- JWT roles: `Admin`, `ApiOwner`, `Tester`, and `Viewer`.

## Technology versions

- .NET / ASP.NET Core 10
- Entity Framework Core 10.0.10
- Oracle.EntityFrameworkCore 10.23.26200
- Oracle Database 19c compatibility through:

```csharp
oracleOptions.UseOracleSQLCompatibility(
    OracleSQLCompatibility.DatabaseVersion19);
```


## Prerequisites

1. .NET 10 SDK.
2. Oracle Database 19c with a dedicated schema/user.
3. Network access from ApiVault to Oracle.
4. An internal TLS certificate for production hosting.
5. A protected, persistent ASP.NET Core Data Protection key store.

## 1. Create the Oracle user

Review and run:

```text
database/01-create-apivault-user.sql
```

Do not use an Oracle built-in administrative account for EF Core migrations.

## 2. Configure secrets

From `src/ApiVault.Api`:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Oracle" "User Id=APIVAULT;Password=<password>;Data Source=<host>:1521/<service>;Pooling=true;Min Pool Size=1;Max Pool Size=50;"
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-random-bytes>"
dotnet user-secrets set "DatabaseInitialization:AdminPassword" "<strong-initial-password>"
```

For production, use environment variables, a secrets manager, or your bank's HSM/vault integration instead of user-secrets.

Environment-variable examples:

```text
ConnectionStrings__Oracle
Jwt__SigningKey
DatabaseInitialization__AdminPassword
```

## 3. Create the EF Core migration

The model and design-time startup wiring are included. Create and apply the first migration:

```bash
dotnet tool install --global dotnet-ef --version 10.0.10

dotnet ef migrations add InitialCreate \
  --project src/ApiVault.Infrastructure \
  --startup-project src/ApiVault.Api \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/ApiVault.Infrastructure \
  --startup-project src/ApiVault.Api
```

Enable one-time reference/admin seeding after the schema exists:

```bash
dotnet user-secrets set "DatabaseInitialization:Enabled" "true"
dotnet user-secrets set "DatabaseInitialization:ApplyMigrations" "false"
dotnet user-secrets set "DatabaseInitialization:SeedAdmin" "true"
```

For controlled non-production deployments, `ApplyMigrations` can be enabled. In production, keep it disabled and run reviewed migration scripts through the bank's database change process.

## 4. Run

```bash
dotnet restore
dotnet run --project src/ApiVault.Api
```

Development URLs:

- API: `https://localhost:7185`
- Health: `https://localhost:7185/health`


## Core workflow

1. Create business areas and development teams.
2. Register vendor companies for externally supplied systems.
3. Register the source system that publishes the API and identify its internal or third-party ownership.
4. Register an API asset under that publishing system.
5. Add an API version/release, endpoints, and Development/UAT/Production environments.
6. Add encrypted environment secrets.
7. Register consumer applications and link the exact API versions they consume.
8. Execute controlled tests and review retained history.
9. Move versions through lifecycle states without deleting governance records.

## Endpoint header JSON

`RequestHeadersJson` is a JSON object. Secret placeholders are resolved from the selected environment at execution time:

```json
{
  "Accept": "application/json",
  "X-IBM-Client-Id": "{{secret:CLIENT_ID}}",
  "X-IBM-Client-Secret": "{{secret:CLIENT_SECRET}}"
}
```

Create the corresponding environment secrets with the names `CLIENT_ID` and `CLIENT_SECRET`.

## Authentication secret conventions

The executor can populate common authentication schemes when these environment secrets exist:

| Authentication type | Secret names |
|---|---|
| Bearer | `BEARER_TOKEN` |
| OAuth2 access token | `OAUTH_ACCESS_TOKEN` |
| Basic | `BASIC_USERNAME`, `BASIC_PASSWORD` |
| API Key | `API_KEY` |

For API Key, `AuthenticationConfigJson` can define the header:

```json
{
  "headerName": "X-API-Key"
}
```

OAuth2 token acquisition and mutual-TLS client-certificate selection are intentionally not automated in this starter. Store instructions and use an approved token/certificate integration before production enablement.

## SSRF policy and internal banking endpoints

The safe default blocks private addresses. Approved internal hosts must be explicitly allowlisted:

```json
{
  "ApiExecutionSecurity": {
    "AllowHttp": false,
    "AllowPrivateNetworks": false,
    "AllowedHosts": [
      "cbs-uat.bank.local",
      "hrm-api.bank.local",
      "*.approved-api.bank.local"
    ]
  }
}
```

Allowlisting is host-based and still permanently blocks known metadata destinations. Production should also enforce an outbound firewall or egress proxy allowlist because application-level SSRF controls are only one layer.

## Data Protection keys

Environment secrets are encrypted using ASP.NET Core Data Protection. The key ring must be shared and backed up when multiple API instances are deployed. Protect the key ring using a certificate, HSM, or bank-approved key-management system. Never commit the `keys` directory.

## Lifecycle rules

Supported forward transitions:

```text
Draft -> Active
Draft -> Retired
Active -> Deprecated
Active -> Retired
Deprecated -> Retired
```

The service intentionally does not expose hard-delete endpoints for catalog, version, project, test-history, or audit data.

## Important production hardening

- Integrate with the bank's identity provider instead of keeping local users as the final authentication model.
- Put ApiVault behind an API gateway/WAF and private network ingress.
- Use reviewed egress allowlists and DNS controls.
- Add four-eyes approval for Production endpoint or secret changes.
- Forward audit events to the bank SIEM.
- Encrypt Oracle tablespaces/backups and enforce least privilege.
- Add retention and masking policies for test payloads because they may contain customer data.
- Add malware/content scanning if file or binary payload support is introduced.
- Add certificate pinning/mTLS and OAuth2 client-credentials integrations where required.
- Add concurrency tokens and approval workflows before enterprise rollout.

## Main API routes

```text
POST   /api/auth/login
PUT    /api/auth/password
GET    /api/apis
POST   /api/apis
PUT    /api/apis/{id}
POST   /api/apis/{apiId}/versions
PUT    /api/apis/{apiId}/versions/{versionId}
PATCH  /api/apis/{apiId}/versions/{versionId}/lifecycle
POST   /api/apis/{apiId}/versions/{versionId}/endpoints
PUT    /api/apis/{apiId}/versions/{versionId}/endpoints/{endpointId}
POST   /api/apis/{apiId}/versions/{versionId}/environments
PUT    /api/apis/{apiId}/versions/{versionId}/environments/{environmentId}
PUT    /api/apis/{apiId}/versions/{versionId}/environments/{environmentId}/secret
GET    /api/projects
GET    /api/projects/{id}
POST   /api/projects
PUT    /api/projects/{id}
POST   /api/projects/{projectId}/api-versions
GET    /api/vendors
POST   /api/vendors
PUT    /api/vendors/{id}
PUT    /api/users/{userId}/password
POST   /api/api-tests/execute
GET    /api/api-tests/history
GET    /api/audit-logs
```
