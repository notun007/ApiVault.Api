using ApiVault.Application.Models;
using ApiVault.Domain.Entities;

namespace ApiVault.Application.Abstractions;

public interface IApiTestExecutor
{
    Task<ApiExecutionResult> ExecuteAsync(ApiExecutionContext context, CancellationToken cancellationToken);
}

public interface IApiAuthenticationTokenProvider
{
    Task<ResolvedAccessToken> GetTokenAsync(
        ApiAuthenticationProfile profile,
        ApiEnvironment environment,
        IReadOnlyDictionary<string, string> secrets,
        bool forceRefresh,
        CancellationToken cancellationToken);

    void Invalidate(Guid profileId, Guid environmentId);
}

public interface IApiAuthenticationProfileResolver
{
    Task<ResolvedAuthentication> ResolveAsync(
        ApiAuthenticationProfile profile,
        ApiEnvironment environment,
        IReadOnlyDictionary<string, string> secrets,
        bool forceRefresh,
        CancellationToken cancellationToken);

    void Invalidate(Guid profileId, Guid environmentId);
}
