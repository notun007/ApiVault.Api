using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Application.Models;
using ApiVault.Domain.Entities;
using ApiVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class ApiTestService(
    IApplicationDbContext dbContext,
    IApiTestExecutor executor,
    ISecretProtector secretProtector,
    IUserContext userContext)
{
    public async Task<TestExecutionResponse> ExecuteAsync(ExecuteApiTestRequest request, CancellationToken cancellationToken)
    {
        var endpoint = await dbContext.ApiEndpoints.AsNoTracking()
            .Include(x => x.ApiVersion)
                .ThenInclude(x => x.ApiAsset)
            .SingleOrDefaultAsync(x => x.Id == request.ApiEndpointId, cancellationToken)
            ?? throw new NotFoundException("API endpoint was not found.");

        var environment = await dbContext.ApiEnvironments.AsNoTracking()
            .Include(x => x.Secrets)
            .SingleOrDefaultAsync(x => x.Id == request.ApiEnvironmentId, cancellationToken)
            ?? throw new NotFoundException("API environment was not found.");

        if (endpoint.ApiVersionId != environment.ApiVersionId)
            throw new RequestValidationException("The endpoint and environment must belong to the same API version.");
        if (!environment.IsEnabled)
            throw new RequestValidationException("The selected environment is disabled.");
        if (endpoint.ApiVersion.LifecycleStatus == ApiLifecycleStatus.Retired)
            throw new RequestValidationException("Retired API versions cannot be executed.");

        var secrets = environment.Secrets.ToDictionary(
            x => x.Name,
            x => secretProtector.Unprotect(x.EncryptedValue),
            StringComparer.OrdinalIgnoreCase);

        var result = await executor.ExecuteAsync(new ApiExecutionContext
        {
            Endpoint = endpoint,
            Version = endpoint.ApiVersion,
            Environment = environment,
            Request = request,
            Secrets = secrets
        }, cancellationToken);

        var entity = new TestExecution
        {
            ApiEndpointId = endpoint.Id,
            ApiEnvironmentId = environment.Id,
            StartedAtUtc = result.StartedAtUtc,
            DurationMilliseconds = result.DurationMilliseconds,
            IsSuccess = result.IsSuccess,
            ResponseStatusCode = result.ResponseStatusCode,
            RequestUrl = result.RequestUrl,
            RequestHeadersJson = result.RequestHeadersJson,
            RequestBody = result.RequestBody,
            RequestSizeBytes = result.RequestSizeBytes,
            ResponseHeadersJson = result.ResponseHeadersJson,
            ResponseBody = result.ResponseBody,
            ResponseSizeBytes = result.ResponseSizeBytes,
            ErrorMessage = result.ErrorMessage,
            CreatedBy = userContext.UserName
        };
        dbContext.TestExecutions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<TestExecutionResponse>> GetHistoryAsync(
        Guid? endpointId,
        Guid? environmentId,
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 200);
        var source = dbContext.TestExecutions.AsNoTracking().AsQueryable();
        if (endpointId.HasValue) source = source.Where(x => x.ApiEndpointId == endpointId.Value);
        if (environmentId.HasValue) source = source.Where(x => x.ApiEnvironmentId == environmentId.Value);

        var entities = await source.OrderByDescending(x => x.StartedAtUtc).Take(take).ToListAsync(cancellationToken);
        return entities.Select(Map).ToList();
    }

    private static TestExecutionResponse Map(TestExecution entity) => new()
    {
        Id = entity.Id,
        ApiEndpointId = entity.ApiEndpointId,
        ApiEnvironmentId = entity.ApiEnvironmentId,
        StartedAtUtc = entity.StartedAtUtc,
        DurationMilliseconds = entity.DurationMilliseconds,
        IsSuccess = entity.IsSuccess,
        ResponseStatusCode = entity.ResponseStatusCode,
        RequestUrl = entity.RequestUrl,
        RequestHeadersJson = entity.RequestHeadersJson,
        RequestBody = entity.RequestBody,
        RequestSizeBytes = entity.RequestSizeBytes,
        ResponseHeadersJson = entity.ResponseHeadersJson,
        ResponseBody = entity.ResponseBody,
        ResponseSizeBytes = entity.ResponseSizeBytes,
        ErrorMessage = entity.ErrorMessage,
        RequestedBy = entity.CreatedBy
    };
}
