using ApiVault.Application.Models;

namespace ApiVault.Application.Abstractions;

public interface IApiTestExecutor
{
    Task<ApiExecutionResult> ExecuteAsync(ApiExecutionContext context, CancellationToken cancellationToken);
}
