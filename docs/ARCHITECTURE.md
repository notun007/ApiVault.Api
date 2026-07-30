# ApiVault architecture

## Clean Architecture dependency direction

```text
ApiVault.Api
  -> ApiVault.Application
  -> ApiVault.Infrastructure

ApiVault.Infrastructure
  -> ApiVault.Application
  -> ApiVault.Domain

ApiVault.Application
  -> ApiVault.Domain

ApiVault.Domain
  -> no project dependency
```

## Main aggregate relationships

```text
BusinessArea 1 --- * ApiAsset * --- 1 DevelopmentTeam
ApiAsset     1 --- * ApiVersion
ApiVersion   1 --- * ApiEndpoint
ApiVersion   1 --- * ApiEnvironment 1 --- * EnvironmentSecret
Project      1 --- * ProjectApiVersion * --- 1 ApiVersion
ApiEndpoint  1 --- * TestExecution * --- 1 ApiEnvironment
```

`ProjectApiVersion` is deliberately linked to the exact release identity, not merely the API asset. This permits impact analysis when a release is deprecated or retired.

## Test-execution trust boundary

1. A user with `Admin`, `ApiOwner`, or `Tester` role requests a test.
2. ApiVault loads the registered endpoint, exact version, selected environment, and encrypted secrets.
3. Secrets are decrypted only in process and substituted into approved headers.
4. The final URL is checked by the SSRF guard.
5. Redirects are disabled, request and response limits are applied, and a timeout is enforced.
6. Headers and known secret values are redacted before the test history is persisted.
7. An outbound firewall or egress proxy remains required as a second control layer.

## Governance model

- Version identifiers are not editable after creation.
- Lifecycle transitions only move forward.
- Catalog records, test history, and audit records have no hard-delete HTTP route.
- Every tracked entity change is written to `AUDIT_LOG` with actor, timestamp, IP, trace ID, and redacted property changes.
- Production deployment should add maker-checker approval, enterprise SSO, SIEM forwarding, and bank-approved key management.
