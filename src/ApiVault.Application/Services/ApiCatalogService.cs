using System.Text.Json;
using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using ApiVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class ApiCatalogService(
    IApplicationDbContext dbContext,
    ISecretProtector secretProtector,
    IApiAuthenticationProfileResolver authenticationResolver)
{
    private static readonly HashSet<string> SupportedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"
    };

    public async Task<PagedResult<ApiSummaryResponse>> SearchAsync(ApiSearchQuery query, CancellationToken cancellationToken)
    {
        var source = dbContext.ApiAssets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            source = source.Where(x => x.Name.ToUpper().Contains(search) ||
                                       x.PublishingApplication.Name.ToUpper().Contains(search) ||
                                       (x.Description != null && x.Description.ToUpper().Contains(search)));
        }

        if (query.OwnershipType.HasValue)
            source = source.Where(x => x.PublishingApplication.OwnershipType == query.OwnershipType.Value);
        if (query.Protocol.HasValue)
            source = source.Where(x => x.Protocol == query.Protocol.Value);
        if (query.BusinessAreaId.HasValue)
            source = source.Where(x => x.BusinessAreaId == query.BusinessAreaId.Value);
        if (query.DevelopmentTeamId.HasValue)
            source = source.Where(x => x.DevelopmentTeamId == query.DevelopmentTeamId.Value);
        if (query.PublishingApplicationId.HasValue)
            source = source.Where(x => x.PublishingApplicationId == query.PublishingApplicationId.Value);
        if (query.LifecycleStatus.HasValue)
            source = source.Where(x => x.Versions.Any(v => v.LifecycleStatus == query.LifecycleStatus.Value));

        var totalCount = await source.CountAsync(cancellationToken);
        source = (query.SortBy, query.SortDescending) switch
        {
            (ApiCatalogSortField.Ownership, false) => source.OrderBy(x => x.PublishingApplication.OwnershipType).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.Ownership, true) => source.OrderByDescending(x => x.PublishingApplication.OwnershipType).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.Protocol, false) => source.OrderBy(x => x.Protocol).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.Protocol, true) => source.OrderByDescending(x => x.Protocol).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.Business, false) => source.OrderBy(x => x.BusinessArea.Name).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.Business, true) => source.OrderByDescending(x => x.BusinessArea.Name).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.CurrentRelease, false) => source.OrderBy(x => x.Versions.Where(v => v.IsCurrent).Select(v => v.Version).FirstOrDefault()).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.CurrentRelease, true) => source.OrderByDescending(x => x.Versions.Where(v => v.IsCurrent).Select(v => v.Version).FirstOrDefault()).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.Versions, false) => source.OrderBy(x => x.Versions.Count).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.Versions, true) => source.OrderByDescending(x => x.Versions.Count).ThenBy(x => x.Name).ThenBy(x => x.Id),
            (ApiCatalogSortField.Name, true) => source.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            _ => source.OrderBy(x => x.Name).ThenBy(x => x.Id)
        };

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ApiSummaryResponse
            {
                Id = x.Id,
                Name = x.Name,
                PublishingApplicationId = x.PublishingApplicationId,
                PublishingApplication = new LookupResponse { Id = x.PublishingApplication.Id, Code = x.PublishingApplication.Code, Name = x.PublishingApplication.Name },
                OwnershipType = x.PublishingApplication.OwnershipType,
                Protocol = x.Protocol,
                BusinessArea = x.BusinessArea.Name,
                DevelopmentTeam = x.DevelopmentTeam.Name,
                CurrentVersion = x.Versions.Where(v => v.IsCurrent).Select(v => v.Version).FirstOrDefault(),
                CurrentLifecycleStatus = x.Versions.Where(v => v.IsCurrent)
                    .Select(v => (ApiLifecycleStatus?)v.LifecycleStatus).FirstOrDefault(),
                VersionCount = x.Versions.Count
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ApiSummaryResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiAssets
            .AsNoTracking()
            .Include(x => x.BusinessArea)
            .Include(x => x.DevelopmentTeam)
            .Include(x => x.PublishingApplication)
                .ThenInclude(x => x.Vendor)
            .Include(x => x.Versions)
                .ThenInclude(x => x.Endpoints)
            .Include(x => x.Versions)
                .ThenInclude(x => x.AuthenticationProfile)
                    .ThenInclude(x => x!.TokenEndpoint)
            .Include(x => x.Versions)
                .ThenInclude(x => x.Environments)
                    .ThenInclude(x => x.Secrets)
            .Include(x => x.Versions)
                .ThenInclude(x => x.ProjectLinks)
                    .ThenInclude(x => x.Project)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("API registration was not found.");

        return MapApiDetail(entity);
    }

    public async Task<ApiDetailResponse> CreateAsync(
        CreateApiRequest request,
        string username,
        CancellationToken cancellationToken)
    {
        var publishingApplication = await GetPublishingApplicationAsync(request.PublishingApplicationId, cancellationToken);
        var creator = await dbContext.AppUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Username.ToUpper() == username.Trim().ToUpper(), cancellationToken)
            ?? throw new RequestValidationException("The authenticated user could not be found.");

        var duplicate = await dbContext.ApiAssets.AnyAsync(
            x => x.Name.ToUpper() == request.Name.Trim().ToUpper() &&
                 x.PublishingApplicationId == request.PublishingApplicationId, cancellationToken);
        if (duplicate)
            throw new ConflictException("An API with the same name and publishing system already exists.");

        var entity = new ApiAsset
        {
            Name = request.Name.Trim(),
            PublishingApplicationId = request.PublishingApplicationId,
            Description = request.Description?.Trim(),
            OwnershipType = publishingApplication.OwnershipType,
            Protocol = request.Protocol,
            CreatorName = creator.DisplayName,
            CreatorEmail = creator.Email,
            VendorName = publishingApplication.Vendor?.Name,
            ExternalReferenceUrl = request.ExternalReferenceUrl?.Trim(),
            BusinessAreaId = publishingApplication.BusinessAreaId!.Value,
            DevelopmentTeamId = publishingApplication.OwnerTeamId!.Value
        };

        dbContext.ApiAssets.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<ApiDetailResponse> UpdateAsync(Guid id, CreateApiRequest request, CancellationToken cancellationToken)
    {
        var publishingApplication = await GetPublishingApplicationAsync(request.PublishingApplicationId, cancellationToken);
        var entity = await dbContext.ApiAssets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("API registration was not found.");

        var duplicate = await dbContext.ApiAssets.AnyAsync(
            x => x.Id != id && x.Name.ToUpper() == request.Name.Trim().ToUpper() &&
                 x.PublishingApplicationId == request.PublishingApplicationId, cancellationToken);
        if (duplicate)
            throw new ConflictException("An API with the same name and publishing system already exists.");

        entity.Name = request.Name.Trim();
        entity.PublishingApplicationId = request.PublishingApplicationId;
        entity.Description = request.Description?.Trim();
        entity.OwnershipType = publishingApplication.OwnershipType;
        entity.Protocol = request.Protocol;
        entity.VendorName = publishingApplication.Vendor?.Name;
        entity.ExternalReferenceUrl = request.ExternalReferenceUrl?.Trim();
        entity.BusinessAreaId = publishingApplication.BusinessAreaId!.Value;
        entity.DevelopmentTeamId = publishingApplication.OwnerTeamId!.Value;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<ApiVersionResponse> AddVersionAsync(Guid apiId, CreateApiVersionRequest request, CancellationToken cancellationToken)
    {
        var api = await dbContext.ApiAssets.Include(x => x.Versions)
            .SingleOrDefaultAsync(x => x.Id == apiId, cancellationToken)
            ?? throw new NotFoundException("API registration was not found.");

        var versionText = request.Version.Trim();
        if (api.Versions.Any(x => string.Equals(x.Version, versionText, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException("This version is already registered for the API.");

        ValidateJson(request.AuthenticationConfigJson, nameof(request.AuthenticationConfigJson));

        if (request.IsCurrent)
        {
            foreach (var version in api.Versions)
                version.IsCurrent = false;
        }

        var entity = new ApiVersion
        {
            ApiAssetId = apiId,
            Version = versionText,
            ReleaseName = request.ReleaseName?.Trim(),
            LifecycleStatus = request.LifecycleStatus,
            ReleaseDateUtc = request.ReleaseDateUtc,
            ChangeLog = request.ChangeLog,
            AuthenticationType = request.AuthenticationType,
            AuthenticationInstructions = request.AuthenticationInstructions,
            AuthenticationConfigJson = request.AuthenticationConfigJson,
            MaxRequestBytes = request.MaxRequestBytes,
            MaxResponseBytes = request.MaxResponseBytes,
            TimeoutSeconds = request.TimeoutSeconds,
            IsCurrent = request.IsCurrent
        };

        ApplyLifecycleDates(entity, request.LifecycleStatus);
        dbContext.ApiVersions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(entity.Id, cancellationToken);
    }

    public async Task<ApiVersionResponse> UpdateVersionAsync(
        Guid apiId,
        Guid versionId,
        UpdateApiVersionRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiVersions.SingleOrDefaultAsync(
            x => x.Id == versionId && x.ApiAssetId == apiId, cancellationToken)
            ?? throw new NotFoundException("API version was not found.");

        ValidateJson(request.AuthenticationConfigJson, nameof(request.AuthenticationConfigJson));
        if (request.IsCurrent && !entity.IsCurrent)
        {
            var currentVersions = await dbContext.ApiVersions
                .Where(x => x.ApiAssetId == apiId && x.Id != versionId && x.IsCurrent)
                .ToListAsync(cancellationToken);
            foreach (var current in currentVersions) current.IsCurrent = false;
        }

        entity.ReleaseName = request.ReleaseName?.Trim();
        entity.ReleaseDateUtc = request.ReleaseDateUtc;
        entity.ChangeLog = request.ChangeLog;
        entity.AuthenticationType = request.AuthenticationType;
        entity.AuthenticationInstructions = request.AuthenticationInstructions;
        entity.AuthenticationConfigJson = request.AuthenticationConfigJson;
        entity.MaxRequestBytes = request.MaxRequestBytes;
        entity.MaxResponseBytes = request.MaxResponseBytes;
        entity.TimeoutSeconds = request.TimeoutSeconds;
        entity.IsCurrent = request.IsCurrent;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(entity.Id, cancellationToken);
    }

    public async Task<ApiVersionResponse> ChangeLifecycleAsync(
        Guid apiId,
        Guid versionId,
        ChangeLifecycleRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiVersions.SingleOrDefaultAsync(
            x => x.Id == versionId && x.ApiAssetId == apiId, cancellationToken)
            ?? throw new NotFoundException("API version was not found.");

        if (!IsValidTransition(entity.LifecycleStatus, request.LifecycleStatus))
            throw new RequestValidationException(
                $"Lifecycle transition from {entity.LifecycleStatus} to {request.LifecycleStatus} is not allowed.");

        entity.LifecycleStatus = request.LifecycleStatus;
        ApplyLifecycleDates(entity, request.LifecycleStatus);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(entity.Id, cancellationToken);
    }

    public async Task<AuthenticationProfileResponse> UpsertAuthenticationProfileAsync(
        Guid apiId,
        Guid versionId,
        UpsertAuthenticationProfileRequest request,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.ApiVersions
            .Include(x => x.AuthenticationProfile)
            .SingleOrDefaultAsync(x => x.Id == versionId && x.ApiAssetId == apiId, cancellationToken)
            ?? throw new NotFoundException("API version was not found.");

        if (!Enum.IsDefined(request.Type))
            throw new RequestValidationException("Authentication profile type is invalid.");

        ApiEndpoint? tokenEndpoint = null;
        if (UsesTokenEndpoint(request.Type))
        {
            if (!request.TokenEndpointId.HasValue)
                throw new RequestValidationException("A token endpoint is required for this authentication profile type.");
            tokenEndpoint = await dbContext.ApiEndpoints.SingleOrDefaultAsync(
                x => x.Id == request.TokenEndpointId.Value && x.ApiVersionId == versionId,
                cancellationToken)
                ?? throw new RequestValidationException("The authentication endpoint must belong to this API version.");
            ValidateJsonPath(request.TokenJsonPath, nameof(request.TokenJsonPath), required: true);
            ValidateJsonPath(request.TokenTypeJsonPath, nameof(request.TokenTypeJsonPath), required: false);
            ValidateJsonPath(request.ExpiresAtJsonPath, nameof(request.ExpiresAtJsonPath), required: false);
            ValidateJsonPath(request.ExpiresInJsonPath, nameof(request.ExpiresInJsonPath), required: false);
        }
        ValidateJsonObject(request.ConfigurationJson, nameof(request.ConfigurationJson));

        var profile = version.AuthenticationProfile;
        if (profile is null)
        {
            profile = new ApiAuthenticationProfile { ApiVersionId = versionId };
            dbContext.ApiAuthenticationProfiles.Add(profile);
        }

        profile.Name = request.Name.Trim();
        profile.Type = request.Type;
        profile.TokenEndpointId = tokenEndpoint?.Id;
        profile.TokenEndpoint = tokenEndpoint;
        profile.TokenJsonPath = request.TokenJsonPath.Trim();
        profile.TokenTypeJsonPath = NormalizeOptional(request.TokenTypeJsonPath);
        profile.ExpiresAtJsonPath = NormalizeOptional(request.ExpiresAtJsonPath);
        profile.ExpiresInJsonPath = NormalizeOptional(request.ExpiresInJsonPath);
        profile.HeaderName = request.HeaderName.Trim();
        profile.ConfigurationJson = NormalizeJson(request.ConfigurationJson);
        profile.RefreshSkewSeconds = request.RefreshSkewSeconds;
        profile.IsEnabled = request.IsEnabled;

        if (tokenEndpoint is not null)
            tokenEndpoint.AuthenticationMode = EndpointAuthenticationMode.Anonymous;
        version.AuthenticationType = MapAuthenticationType(request.Type);

        await dbContext.SaveChangesAsync(cancellationToken);
        var environmentIds = await dbContext.ApiEnvironments.AsNoTracking()
            .Where(x => x.ApiVersionId == versionId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        foreach (var environmentId in environmentIds)
            authenticationResolver.Invalidate(profile.Id, environmentId);
        return MapAuthenticationProfile(profile);
    }

    public async Task<AuthenticationProfileTestResponse> TestAuthenticationProfileAsync(
        Guid apiId,
        Guid versionId,
        TestAuthenticationProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.ApiAuthenticationProfiles.AsNoTracking()
            .Include(x => x.ApiVersion)
            .Include(x => x.TokenEndpoint)
            .SingleOrDefaultAsync(
                x => x.ApiVersionId == versionId && x.ApiVersion.ApiAssetId == apiId,
                cancellationToken)
            ?? throw new NotFoundException("Authentication profile was not found.");

        if (!profile.IsEnabled)
            throw new RequestValidationException("Authentication profile is disabled.");

        var environment = await dbContext.ApiEnvironments.AsNoTracking()
            .Include(x => x.Secrets)
            .SingleOrDefaultAsync(
                x => x.Id == request.ApiEnvironmentId && x.ApiVersionId == versionId,
                cancellationToken)
            ?? throw new RequestValidationException("The selected environment does not belong to this API version.");

        var secrets = environment.Secrets.ToDictionary(
            x => x.Name,
            x => secretProtector.Unprotect(x.EncryptedValue),
            StringComparer.OrdinalIgnoreCase);
        var authentication = await authenticationResolver.ResolveAsync(profile, environment, secrets, true, cancellationToken);

        return new AuthenticationProfileTestResponse
        {
            IsSuccess = true,
            Type = profile.Type,
            Summary = authentication.Summary,
            TokenType = authentication.TokenType,
            ExpiresAtUtc = authentication.ExpiresAtUtc,
            FromCache = authentication.FromCache
        };
    }

    public async Task<EndpointResponse> AddEndpointAsync(
        Guid apiId,
        Guid versionId,
        CreateEndpointRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureVersionBelongsToApiAsync(apiId, versionId, cancellationToken);

        var method = request.HttpMethod.Trim().ToUpperInvariant();
        if (!SupportedMethods.Contains(method))
            throw new RequestValidationException($"HTTP method '{method}' is not supported.");
        if (!Enum.IsDefined(request.AuthenticationMode))
            throw new RequestValidationException("AuthenticationMode is invalid.");
        if (Uri.TryCreate(request.RelativePath, UriKind.Absolute, out _))
            throw new RequestValidationException("RelativePath must not contain a scheme or host.");

        ValidateJson(request.RequestHeadersJson, nameof(request.RequestHeadersJson));
        ValidateJson(request.QueryParametersJson, nameof(request.QueryParametersJson));
        ValidateJson(request.PathParametersJson, nameof(request.PathParametersJson));
        ValidateJson(request.ResponseHeadersSampleJson, nameof(request.ResponseHeadersSampleJson));
        ValidateJson(request.SuccessStatusCodesJson, nameof(request.SuccessStatusCodesJson));

        var entity = new ApiEndpoint
        {
            ApiVersionId = versionId,
            Name = request.Name.Trim(),
            RelativePath = request.RelativePath.Trim(),
            HttpMethod = method,
            Description = request.Description?.Trim(),
            RequestHeadersJson = request.RequestHeadersJson,
            QueryParametersJson = request.QueryParametersJson,
            PathParametersJson = request.PathParametersJson,
            RequestPayloadSample = request.RequestPayloadSample,
            ResponseHeadersSampleJson = request.ResponseHeadersSampleJson,
            ResponseBodySample = request.ResponseBodySample,
            SuccessStatusCodesJson = request.SuccessStatusCodesJson,
            SoapAction = request.SoapAction?.Trim(),
            AuthenticationMode = request.AuthenticationMode
        };

        dbContext.ApiEndpoints.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapEndpoint(entity);
    }

    public async Task<EndpointResponse> UpdateEndpointAsync(
        Guid apiId,
        Guid versionId,
        Guid endpointId,
        CreateEndpointRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiEndpoints.SingleOrDefaultAsync(
            x => x.Id == endpointId && x.ApiVersionId == versionId && x.ApiVersion.ApiAssetId == apiId,
            cancellationToken)
            ?? throw new NotFoundException("API endpoint was not found.");

        var method = request.HttpMethod.Trim().ToUpperInvariant();
        if (!SupportedMethods.Contains(method))
            throw new RequestValidationException($"HTTP method '{method}' is not supported.");
        if (!Enum.IsDefined(request.AuthenticationMode))
            throw new RequestValidationException("AuthenticationMode is invalid.");
        if (request.AuthenticationMode != EndpointAuthenticationMode.Anonymous &&
            await dbContext.ApiAuthenticationProfiles.AnyAsync(x => x.TokenEndpointId == endpointId, cancellationToken))
            throw new RequestValidationException("The configured authentication endpoint must remain anonymous.");
        if (Uri.TryCreate(request.RelativePath, UriKind.Absolute, out _))
            throw new RequestValidationException("RelativePath must not contain a scheme or host.");
        ValidateJson(request.RequestHeadersJson, nameof(request.RequestHeadersJson));
        ValidateJson(request.QueryParametersJson, nameof(request.QueryParametersJson));
        ValidateJson(request.PathParametersJson, nameof(request.PathParametersJson));
        ValidateJson(request.ResponseHeadersSampleJson, nameof(request.ResponseHeadersSampleJson));
        ValidateJson(request.SuccessStatusCodesJson, nameof(request.SuccessStatusCodesJson));

        entity.Name = request.Name.Trim();
        entity.RelativePath = request.RelativePath.Trim();
        entity.HttpMethod = method;
        entity.Description = request.Description?.Trim();
        entity.RequestHeadersJson = request.RequestHeadersJson;
        entity.QueryParametersJson = request.QueryParametersJson;
        entity.PathParametersJson = request.PathParametersJson;
        entity.RequestPayloadSample = request.RequestPayloadSample;
        entity.ResponseHeadersSampleJson = request.ResponseHeadersSampleJson;
        entity.ResponseBodySample = request.ResponseBodySample;
        entity.SuccessStatusCodesJson = request.SuccessStatusCodesJson;
        entity.SoapAction = request.SoapAction?.Trim();
        entity.AuthenticationMode = request.AuthenticationMode;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapEndpoint(entity);
    }

    public async Task<EnvironmentResponse> AddEnvironmentAsync(
        Guid apiId,
        Guid versionId,
        CreateEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureVersionBelongsToApiAsync(apiId, versionId, cancellationToken);

        if (!Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new RequestValidationException("BaseUrl must be an absolute HTTP or HTTPS URL.");

        var duplicate = await dbContext.ApiEnvironments.AnyAsync(
            x => x.ApiVersionId == versionId && x.EnvironmentType == request.EnvironmentType, cancellationToken);
        if (duplicate)
            throw new ConflictException("This environment is already registered for the API version.");

        var entity = new ApiEnvironment
        {
            ApiVersionId = versionId,
            EnvironmentType = request.EnvironmentType,
            BaseUrl = request.BaseUrl.TrimEnd('/'),
            IsEnabled = request.IsEnabled,
            Notes = request.Notes?.Trim()
        };

        dbContext.ApiEnvironments.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapEnvironment(entity);
    }

    public async Task<EnvironmentResponse> UpdateEnvironmentAsync(
        Guid apiId,
        Guid versionId,
        Guid environmentId,
        CreateEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiEnvironments.Include(x => x.Secrets).SingleOrDefaultAsync(
            x => x.Id == environmentId && x.ApiVersionId == versionId && x.ApiVersion.ApiAssetId == apiId,
            cancellationToken)
            ?? throw new NotFoundException("API environment was not found.");

        if (!Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new RequestValidationException("BaseUrl must be an absolute HTTP or HTTPS URL.");
        var duplicate = await dbContext.ApiEnvironments.AnyAsync(
            x => x.Id != environmentId && x.ApiVersionId == versionId && x.EnvironmentType == request.EnvironmentType,
            cancellationToken);
        if (duplicate)
            throw new ConflictException("This environment is already registered for the API version.");

        entity.EnvironmentType = request.EnvironmentType;
        entity.BaseUrl = request.BaseUrl.TrimEnd('/');
        entity.IsEnabled = request.IsEnabled;
        entity.Notes = request.Notes?.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapEnvironment(entity);
    }

    public async Task<EnvironmentResponse> SetEnvironmentSecretAsync(
        Guid apiId,
        Guid versionId,
        Guid environmentId,
        SetEnvironmentSecretRequest request,
        CancellationToken cancellationToken)
    {
        var environment = await dbContext.ApiEnvironments
            .Include(x => x.Secrets)
            .SingleOrDefaultAsync(
                x => x.Id == environmentId && x.ApiVersionId == versionId && x.ApiVersion.ApiAssetId == apiId,
                cancellationToken)
            ?? throw new NotFoundException("API environment was not found.");

        var name = request.Name.Trim().ToUpperInvariant();
        var existing = environment.Secrets.SingleOrDefault(
            x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new EnvironmentSecret
            {
                ApiEnvironmentId = environmentId,
                Name = name,
                EncryptedValue = secretProtector.Protect(request.Value)
            };
            dbContext.EnvironmentSecrets.Add(existing);
        }
        else
        {
            existing.EncryptedValue = secretProtector.Protect(request.Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var profileId = await dbContext.ApiAuthenticationProfiles.AsNoTracking()
            .Where(x => x.ApiVersionId == versionId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (profileId.HasValue) authenticationResolver.Invalidate(profileId.Value, environmentId);
        return await GetEnvironmentAsync(environmentId, cancellationToken);
    }

    public async Task<ApiVersionResponse> GetVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiVersions.AsNoTracking()
            .Include(x => x.Endpoints)
            .Include(x => x.AuthenticationProfile)
                .ThenInclude(x => x!.TokenEndpoint)
            .Include(x => x.Environments)
                .ThenInclude(x => x.Secrets)
            .SingleOrDefaultAsync(x => x.Id == versionId, cancellationToken)
            ?? throw new NotFoundException("API version was not found.");
        return MapVersion(entity);
    }

    private async Task<EnvironmentResponse> GetEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var environment = await dbContext.ApiEnvironments.AsNoTracking()
            .Include(x => x.Secrets)
            .SingleAsync(x => x.Id == environmentId, cancellationToken);
        return MapEnvironment(environment);
    }

    private async Task EnsureLookupValuesExistAsync(Guid businessAreaId, Guid teamId, CancellationToken cancellationToken)
    {
        if (!await dbContext.BusinessAreas.AnyAsync(x => x.Id == businessAreaId, cancellationToken))
            throw new RequestValidationException("Business area does not exist.");
        if (!await dbContext.DevelopmentTeams.AnyAsync(x => x.Id == teamId, cancellationToken))
            throw new RequestValidationException("Development team does not exist.");
    }

    private async Task EnsureVersionBelongsToApiAsync(Guid apiId, Guid versionId, CancellationToken cancellationToken)
    {
        if (!await dbContext.ApiVersions.AnyAsync(x => x.Id == versionId && x.ApiAssetId == apiId, cancellationToken))
            throw new NotFoundException("API version was not found.");
    }

    private static void ValidateJson(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try { JsonDocument.Parse(value); }
        catch (JsonException ex) { throw new RequestValidationException($"{fieldName} must contain valid JSON: {ex.Message}"); }
    }

    private static void ValidateJsonObject(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new RequestValidationException($"{fieldName} must contain a JSON object.");
        }
        catch (JsonException ex)
        {
            throw new RequestValidationException($"{fieldName} must contain valid JSON: {ex.Message}");
        }
    }

    private static string? NormalizeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        using var document = JsonDocument.Parse(value);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static bool UsesTokenEndpoint(AuthenticationProfileType type) =>
        type is AuthenticationProfileType.TokenEndpoint or AuthenticationProfileType.OAuth2ClientCredentials;

    private static AuthenticationType MapAuthenticationType(AuthenticationProfileType type) => type switch
    {
        AuthenticationProfileType.Basic => AuthenticationType.Basic,
        AuthenticationProfileType.ApiKey => AuthenticationType.ApiKey,
        AuthenticationProfileType.OAuth2ClientCredentials => AuthenticationType.OAuth2,
        AuthenticationProfileType.MutualTls => AuthenticationType.MutualTls,
        AuthenticationProfileType.CustomHeaders => AuthenticationType.Custom,
        _ => AuthenticationType.Bearer
    };

    private static void ValidateJsonPath(string? value, string fieldName, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required) throw new RequestValidationException($"{fieldName} is required.");
            return;
        }

        var path = value.Trim();
        if (!path.StartsWith("$.", StringComparison.Ordinal) ||
            path.Length <= 2 ||
            path[2..].Split('.').Any(string.IsNullOrWhiteSpace))
            throw new RequestValidationException($"{fieldName} must be a simple JSON path such as $.accessToken.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsValidTransition(ApiLifecycleStatus current, ApiLifecycleStatus next) =>
        current == next || (current, next) switch
        {
            (ApiLifecycleStatus.Draft, ApiLifecycleStatus.Active) => true,
            (ApiLifecycleStatus.Draft, ApiLifecycleStatus.Retired) => true,
            (ApiLifecycleStatus.Active, ApiLifecycleStatus.Deprecated) => true,
            (ApiLifecycleStatus.Active, ApiLifecycleStatus.Retired) => true,
            (ApiLifecycleStatus.Deprecated, ApiLifecycleStatus.Retired) => true,
            _ => false
        };

    private static void ApplyLifecycleDates(ApiVersion entity, ApiLifecycleStatus status)
    {
        var now = DateTime.UtcNow;
        if (status == ApiLifecycleStatus.Active)
            entity.ReleaseDateUtc ??= now;
        if (status == ApiLifecycleStatus.Deprecated)
            entity.DeprecatedAtUtc ??= now;
        if (status == ApiLifecycleStatus.Retired)
            entity.RetiredAtUtc ??= now;
    }

    private async Task<Project> GetPublishingApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await dbContext.Projects.AsNoTracking().Include(x => x.Vendor)
            .SingleOrDefaultAsync(x => x.Id == applicationId && x.Status == ProjectStatus.Active, cancellationToken)
            ?? throw new RequestValidationException("Publishing application does not exist or is inactive.");
        if (!application.BusinessAreaId.HasValue || !application.OwnerTeamId.HasValue)
            throw new RequestValidationException("Complete the publishing application's business area and owner team before registering an API.");
        return application;
    }

    private static ApiDetailResponse MapApiDetail(ApiAsset entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        PublishingApplicationId = entity.PublishingApplicationId,
        PublishingApplication = new LookupResponse { Id = entity.PublishingApplication.Id, Code = entity.PublishingApplication.Code, Name = entity.PublishingApplication.Name },
        Description = entity.Description,
        OwnershipType = entity.PublishingApplication.OwnershipType,
        Protocol = entity.Protocol,
        CreatorName = entity.CreatorName,
        CreatorEmail = entity.CreatorEmail,
        VendorName = entity.PublishingApplication.Vendor?.Name,
        ExternalReferenceUrl = entity.ExternalReferenceUrl,
        BusinessArea = new LookupResponse { Id = entity.BusinessArea.Id, Code = entity.BusinessArea.Code, Name = entity.BusinessArea.Name },
        DevelopmentTeam = new LookupResponse { Id = entity.DevelopmentTeam.Id, Code = entity.DevelopmentTeam.Code, Name = entity.DevelopmentTeam.Name },
        Versions = entity.Versions.OrderByDescending(x => x.IsCurrent).ThenByDescending(x => x.CreatedAtUtc).Select(MapVersion).ToList()
    };

    private static ApiVersionResponse MapVersion(ApiVersion entity) => new()
    {
        Id = entity.Id,
        Version = entity.Version,
        ReleaseName = entity.ReleaseName,
        LifecycleStatus = entity.LifecycleStatus,
        ReleaseDateUtc = entity.ReleaseDateUtc,
        DeprecatedAtUtc = entity.DeprecatedAtUtc,
        RetiredAtUtc = entity.RetiredAtUtc,
        ChangeLog = entity.ChangeLog,
        AuthenticationType = entity.AuthenticationType,
        AuthenticationInstructions = entity.AuthenticationInstructions,
        AuthenticationConfigJson = entity.AuthenticationConfigJson,
        MaxRequestBytes = entity.MaxRequestBytes,
        MaxResponseBytes = entity.MaxResponseBytes,
        TimeoutSeconds = entity.TimeoutSeconds,
        IsCurrent = entity.IsCurrent,
        AuthenticationProfile = entity.AuthenticationProfile is null
            ? null
            : MapAuthenticationProfile(entity.AuthenticationProfile),
        Consumers = entity.ProjectLinks.OrderBy(x => x.Project.Name).Select(x => new ApiVersionConsumerResponse
        {
            LinkId = x.Id,
            ProjectId = x.ProjectId,
            ProjectCode = x.Project.Code,
            ProjectName = x.Project.Name,
            Purpose = x.Purpose,
            IsRequired = x.IsRequired
        }).ToList(),
        Endpoints = entity.Endpoints.OrderBy(x => x.RelativePath).ThenBy(x => x.HttpMethod).Select(MapEndpoint).ToList(),
        Environments = entity.Environments.OrderBy(x => x.EnvironmentType).Select(MapEnvironment).ToList()
    };

    private static EndpointResponse MapEndpoint(ApiEndpoint entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        RelativePath = entity.RelativePath,
        HttpMethod = entity.HttpMethod,
        Description = entity.Description,
        RequestHeadersJson = entity.RequestHeadersJson,
        QueryParametersJson = entity.QueryParametersJson,
        PathParametersJson = entity.PathParametersJson,
        RequestPayloadSample = entity.RequestPayloadSample,
        ResponseHeadersSampleJson = entity.ResponseHeadersSampleJson,
        ResponseBodySample = entity.ResponseBodySample,
        SuccessStatusCodesJson = entity.SuccessStatusCodesJson,
        SoapAction = entity.SoapAction,
        AuthenticationMode = entity.AuthenticationMode
    };

    private static AuthenticationProfileResponse MapAuthenticationProfile(ApiAuthenticationProfile entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Type = entity.Type,
        TokenEndpointId = entity.TokenEndpointId,
        TokenEndpointName = entity.TokenEndpoint?.Name,
        TokenEndpointMethod = entity.TokenEndpoint?.HttpMethod,
        TokenEndpointPath = entity.TokenEndpoint?.RelativePath,
        TokenJsonPath = entity.TokenJsonPath,
        TokenTypeJsonPath = entity.TokenTypeJsonPath,
        ExpiresAtJsonPath = entity.ExpiresAtJsonPath,
        ExpiresInJsonPath = entity.ExpiresInJsonPath,
        HeaderName = entity.HeaderName,
        ConfigurationJson = entity.ConfigurationJson,
        RefreshSkewSeconds = entity.RefreshSkewSeconds,
        IsEnabled = entity.IsEnabled
    };

    private static EnvironmentResponse MapEnvironment(ApiEnvironment entity) => new()
    {
        Id = entity.Id,
        EnvironmentType = entity.EnvironmentType,
        BaseUrl = entity.BaseUrl,
        IsEnabled = entity.IsEnabled,
        Notes = entity.Notes,
        SecretNames = entity.Secrets.OrderBy(x => x.Name).Select(x => x.Name).ToList()
    };
}



//Commented on 26-072026
//using System.Text.Json;
//using ApiVault.Application.Abstractions;
//using ApiVault.Application.Common;
//using ApiVault.Application.DTOs;
//using ApiVault.Domain.Entities;
//using ApiVault.Domain.Enums;
//using Microsoft.EntityFrameworkCore;

//namespace ApiVault.Application.Services;

//public sealed class ApiCatalogService(IApplicationDbContext dbContext, ISecretProtector secretProtector)
//{
//    private static readonly HashSet<string> SupportedMethods = new(StringComparer.OrdinalIgnoreCase)
//    {
//        "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"
//    };

//    public async Task<PagedResult<ApiSummaryResponse>> SearchAsync(ApiSearchQuery query, CancellationToken cancellationToken)
//    {
//        var source = dbContext.ApiAssets.AsNoTracking().AsQueryable();

//        if (!string.IsNullOrWhiteSpace(query.Search))
//        {
//            var search = query.Search.Trim().ToUpperInvariant();
//            source = source.Where(x => x.Name.ToUpper().Contains(search) ||
//                                       x.ApiProjectName.ToUpper().Contains(search) ||
//                                       (x.Description != null && x.Description.ToUpper().Contains(search)));
//        }

//        if (query.OwnershipType.HasValue)
//            source = source.Where(x => x.OwnershipType == query.OwnershipType.Value);
//        if (query.Protocol.HasValue)
//            source = source.Where(x => x.Protocol == query.Protocol.Value);
//        if (query.BusinessAreaId.HasValue)
//            source = source.Where(x => x.BusinessAreaId == query.BusinessAreaId.Value);
//        if (query.DevelopmentTeamId.HasValue)
//            source = source.Where(x => x.DevelopmentTeamId == query.DevelopmentTeamId.Value);
//        if (query.LifecycleStatus.HasValue)
//            source = source.Where(x => x.Versions.Any(v => v.LifecycleStatus == query.LifecycleStatus.Value));

//        var totalCount = await source.CountAsync(cancellationToken);
//        var items = await source
//            .OrderBy(x => x.Name)
//            .Skip((query.Page - 1) * query.PageSize)
//            .Take(query.PageSize)
//            .Select(x => new ApiSummaryResponse
//            {
//                Id = x.Id,
//                Name = x.Name,
//                ApiProjectName = x.ApiProjectName,
//                OwnershipType = x.OwnershipType,
//                Protocol = x.Protocol,
//                BusinessArea = x.BusinessArea.Name,
//                DevelopmentTeam = x.DevelopmentTeam.Name,
//                CurrentVersion = x.Versions.Where(v => v.IsCurrent).Select(v => v.Version).FirstOrDefault(),
//                CurrentLifecycleStatus = x.Versions.Where(v => v.IsCurrent)
//                    .Select(v => (ApiLifecycleStatus?)v.LifecycleStatus).FirstOrDefault(),
//                VersionCount = x.Versions.Count
//            })
//            .ToListAsync(cancellationToken);

//        return new PagedResult<ApiSummaryResponse>
//        {
//            Items = items,
//            Page = query.Page,
//            PageSize = query.PageSize,
//            TotalCount = totalCount
//        };
//    }

//    public async Task<ApiDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken)
//    {
//        var entity = await dbContext.ApiAssets
//            .AsNoTracking()
//            .Include(x => x.BusinessArea)
//            .Include(x => x.DevelopmentTeam)
//            .Include(x => x.Versions)
//                .ThenInclude(x => x.Endpoints)
//            .Include(x => x.Versions)
//                .ThenInclude(x => x.Environments)
//                    .ThenInclude(x => x.Secrets)
//            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
//            ?? throw new NotFoundException("API registration was not found.");

//        return MapApiDetail(entity);
//    }

//    public async Task<ApiDetailResponse> CreateAsync(CreateApiRequest request, CancellationToken cancellationToken)
//    {
//        await EnsureLookupValuesExistAsync(request.BusinessAreaId, request.DevelopmentTeamId, cancellationToken);

//        var duplicate = await dbContext.ApiAssets.AnyAsync(
//            x => x.Name.ToUpper() == request.Name.Trim().ToUpper() &&
//                 x.ApiProjectName.ToUpper() == request.ApiProjectName.Trim().ToUpper(), cancellationToken);
//        if (duplicate)
//            throw new ConflictException("An API with the same name and API project name already exists.");

//        var entity = new ApiAsset
//        {
//            Name = request.Name.Trim(),
//            ApiProjectName = request.ApiProjectName.Trim(),
//            Description = request.Description?.Trim(),
//            OwnershipType = request.OwnershipType,
//            Protocol = request.Protocol,
//            CreatorName = request.CreatorName.Trim(),
//            CreatorEmail = request.CreatorEmail?.Trim(),
//            VendorName = request.VendorName?.Trim(),
//            ExternalReferenceUrl = request.ExternalReferenceUrl?.Trim(),
//            BusinessAreaId = request.BusinessAreaId,
//            DevelopmentTeamId = request.DevelopmentTeamId
//        };

//        dbContext.ApiAssets.Add(entity);
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return await GetAsync(entity.Id, cancellationToken);
//    }

//    public async Task<ApiDetailResponse> UpdateAsync(Guid id, CreateApiRequest request, CancellationToken cancellationToken)
//    {
//        await EnsureLookupValuesExistAsync(request.BusinessAreaId, request.DevelopmentTeamId, cancellationToken);
//        var entity = await dbContext.ApiAssets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
//            ?? throw new NotFoundException("API registration was not found.");

//        var duplicate = await dbContext.ApiAssets.AnyAsync(
//            x => x.Id != id && x.Name.ToUpper() == request.Name.Trim().ToUpper() &&
//                 x.ApiProjectName.ToUpper() == request.ApiProjectName.Trim().ToUpper(), cancellationToken);
//        if (duplicate)
//            throw new ConflictException("An API with the same name and API project name already exists.");

//        entity.Name = request.Name.Trim();
//        entity.ApiProjectName = request.ApiProjectName.Trim();
//        entity.Description = request.Description?.Trim();
//        entity.OwnershipType = request.OwnershipType;
//        entity.Protocol = request.Protocol;
//        entity.CreatorName = request.CreatorName.Trim();
//        entity.CreatorEmail = request.CreatorEmail?.Trim();
//        entity.VendorName = request.VendorName?.Trim();
//        entity.ExternalReferenceUrl = request.ExternalReferenceUrl?.Trim();
//        entity.BusinessAreaId = request.BusinessAreaId;
//        entity.DevelopmentTeamId = request.DevelopmentTeamId;
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return await GetAsync(id, cancellationToken);
//    }

//    public async Task<ApiVersionResponse> AddVersionAsync(Guid apiId, CreateApiVersionRequest request, CancellationToken cancellationToken)
//    {
//        var api = await dbContext.ApiAssets.Include(x => x.Versions)
//            .SingleOrDefaultAsync(x => x.Id == apiId, cancellationToken)
//            ?? throw new NotFoundException("API registration was not found.");

//        var versionText = request.Version.Trim();
//        if (api.Versions.Any(x => string.Equals(x.Version, versionText, StringComparison.OrdinalIgnoreCase)))
//            throw new ConflictException("This version is already registered for the API.");

//        ValidateJson(request.AuthenticationConfigJson, nameof(request.AuthenticationConfigJson));

//        if (request.IsCurrent)
//        {
//            foreach (var version in api.Versions)
//                version.IsCurrent = false;
//        }

//        var entity = new ApiVersion
//        {
//            ApiAssetId = apiId,
//            Version = versionText,
//            ReleaseName = request.ReleaseName?.Trim(),
//            LifecycleStatus = request.LifecycleStatus,
//            ReleaseDateUtc = request.ReleaseDateUtc,
//            ChangeLog = request.ChangeLog,
//            AuthenticationType = request.AuthenticationType,
//            AuthenticationInstructions = request.AuthenticationInstructions,
//            AuthenticationConfigJson = request.AuthenticationConfigJson,
//            MaxRequestBytes = request.MaxRequestBytes,
//            MaxResponseBytes = request.MaxResponseBytes,
//            TimeoutSeconds = request.TimeoutSeconds,
//            IsCurrent = request.IsCurrent
//        };

//        ApplyLifecycleDates(entity, request.LifecycleStatus);
//        dbContext.ApiVersions.Add(entity);
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return await GetVersionAsync(entity.Id, cancellationToken);
//    }

//    public async Task<ApiVersionResponse> UpdateVersionAsync(
//        Guid apiId,
//        Guid versionId,
//        UpdateApiVersionRequest request,
//        CancellationToken cancellationToken)
//    {
//        var entity = await dbContext.ApiVersions.SingleOrDefaultAsync(
//            x => x.Id == versionId && x.ApiAssetId == apiId, cancellationToken)
//            ?? throw new NotFoundException("API version was not found.");

//        ValidateJson(request.AuthenticationConfigJson, nameof(request.AuthenticationConfigJson));
//        if (request.IsCurrent && !entity.IsCurrent)
//        {
//            var currentVersions = await dbContext.ApiVersions
//                .Where(x => x.ApiAssetId == apiId && x.Id != versionId && x.IsCurrent)
//                .ToListAsync(cancellationToken);
//            foreach (var current in currentVersions) current.IsCurrent = false;
//        }

//        entity.ReleaseName = request.ReleaseName?.Trim();
//        entity.ReleaseDateUtc = request.ReleaseDateUtc;
//        entity.ChangeLog = request.ChangeLog;
//        entity.AuthenticationType = request.AuthenticationType;
//        entity.AuthenticationInstructions = request.AuthenticationInstructions;
//        entity.AuthenticationConfigJson = request.AuthenticationConfigJson;
//        entity.MaxRequestBytes = request.MaxRequestBytes;
//        entity.MaxResponseBytes = request.MaxResponseBytes;
//        entity.TimeoutSeconds = request.TimeoutSeconds;
//        entity.IsCurrent = request.IsCurrent;
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return await GetVersionAsync(entity.Id, cancellationToken);
//    }

//    public async Task<ApiVersionResponse> ChangeLifecycleAsync(
//        Guid apiId,
//        Guid versionId,
//        ChangeLifecycleRequest request,
//        CancellationToken cancellationToken)
//    {
//        var entity = await dbContext.ApiVersions.SingleOrDefaultAsync(
//            x => x.Id == versionId && x.ApiAssetId == apiId, cancellationToken)
//            ?? throw new NotFoundException("API version was not found.");

//        if (!IsValidTransition(entity.LifecycleStatus, request.LifecycleStatus))
//            throw new RequestValidationException(
//                $"Lifecycle transition from {entity.LifecycleStatus} to {request.LifecycleStatus} is not allowed.");

//        entity.LifecycleStatus = request.LifecycleStatus;
//        ApplyLifecycleDates(entity, request.LifecycleStatus);
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return await GetVersionAsync(entity.Id, cancellationToken);
//    }

//    public async Task<EndpointResponse> AddEndpointAsync(
//        Guid apiId,
//        Guid versionId,
//        CreateEndpointRequest request,
//        CancellationToken cancellationToken)
//    {
//        await EnsureVersionBelongsToApiAsync(apiId, versionId, cancellationToken);

//        var method = request.HttpMethod.Trim().ToUpperInvariant();
//        if (!SupportedMethods.Contains(method))
//            throw new RequestValidationException($"HTTP method '{method}' is not supported.");
//        if (Uri.TryCreate(request.RelativePath, UriKind.Absolute, out _))
//            throw new RequestValidationException("RelativePath must not contain a scheme or host.");

//        ValidateJson(request.RequestHeadersJson, nameof(request.RequestHeadersJson));
//        ValidateJson(request.QueryParametersJson, nameof(request.QueryParametersJson));
//        ValidateJson(request.PathParametersJson, nameof(request.PathParametersJson));
//        ValidateJson(request.ResponseHeadersSampleJson, nameof(request.ResponseHeadersSampleJson));
//        ValidateJson(request.SuccessStatusCodesJson, nameof(request.SuccessStatusCodesJson));

//        var entity = new ApiEndpoint
//        {
//            ApiVersionId = versionId,
//            Name = request.Name.Trim(),
//            RelativePath = request.RelativePath.Trim(),
//            HttpMethod = method,
//            Description = request.Description?.Trim(),
//            RequestHeadersJson = request.RequestHeadersJson,
//            QueryParametersJson = request.QueryParametersJson,
//            PathParametersJson = request.PathParametersJson,
//            RequestPayloadSample = request.RequestPayloadSample,
//            ResponseHeadersSampleJson = request.ResponseHeadersSampleJson,
//            ResponseBodySample = request.ResponseBodySample,
//            SuccessStatusCodesJson = request.SuccessStatusCodesJson,
//            SoapAction = request.SoapAction?.Trim()
//        };

//        dbContext.ApiEndpoints.Add(entity);
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return MapEndpoint(entity);
//    }

//    public async Task<EndpointResponse> UpdateEndpointAsync(
//        Guid apiId,
//        Guid versionId,
//        Guid endpointId,
//        CreateEndpointRequest request,
//        CancellationToken cancellationToken)
//    {
//        var entity = await dbContext.ApiEndpoints.SingleOrDefaultAsync(
//            x => x.Id == endpointId && x.ApiVersionId == versionId && x.ApiVersion.ApiAssetId == apiId,
//            cancellationToken)
//            ?? throw new NotFoundException("API endpoint was not found.");

//        var method = request.HttpMethod.Trim().ToUpperInvariant();
//        if (!SupportedMethods.Contains(method))
//            throw new RequestValidationException($"HTTP method '{method}' is not supported.");
//        if (Uri.TryCreate(request.RelativePath, UriKind.Absolute, out _))
//            throw new RequestValidationException("RelativePath must not contain a scheme or host.");
//        ValidateJson(request.RequestHeadersJson, nameof(request.RequestHeadersJson));
//        ValidateJson(request.QueryParametersJson, nameof(request.QueryParametersJson));
//        ValidateJson(request.PathParametersJson, nameof(request.PathParametersJson));
//        ValidateJson(request.ResponseHeadersSampleJson, nameof(request.ResponseHeadersSampleJson));
//        ValidateJson(request.SuccessStatusCodesJson, nameof(request.SuccessStatusCodesJson));

//        entity.Name = request.Name.Trim();
//        entity.RelativePath = request.RelativePath.Trim();
//        entity.HttpMethod = method;
//        entity.Description = request.Description?.Trim();
//        entity.RequestHeadersJson = request.RequestHeadersJson;
//        entity.QueryParametersJson = request.QueryParametersJson;
//        entity.PathParametersJson = request.PathParametersJson;
//        entity.RequestPayloadSample = request.RequestPayloadSample;
//        entity.ResponseHeadersSampleJson = request.ResponseHeadersSampleJson;
//        entity.ResponseBodySample = request.ResponseBodySample;
//        entity.SuccessStatusCodesJson = request.SuccessStatusCodesJson;
//        entity.SoapAction = request.SoapAction?.Trim();
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return MapEndpoint(entity);
//    }

//    public async Task<EnvironmentResponse> AddEnvironmentAsync(
//        Guid apiId,
//        Guid versionId,
//        CreateEnvironmentRequest request,
//        CancellationToken cancellationToken)
//    {
//        await EnsureVersionBelongsToApiAsync(apiId, versionId, cancellationToken);

//        if (!Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out var uri) ||
//            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
//            throw new RequestValidationException("BaseUrl must be an absolute HTTP or HTTPS URL.");

//        var duplicate = await dbContext.ApiEnvironments.AnyAsync(
//            x => x.ApiVersionId == versionId && x.EnvironmentType == request.EnvironmentType, cancellationToken);
//        if (duplicate)
//            throw new ConflictException("This environment is already registered for the API version.");

//        var entity = new ApiEnvironment
//        {
//            ApiVersionId = versionId,
//            EnvironmentType = request.EnvironmentType,
//            BaseUrl = request.BaseUrl.TrimEnd('/'),
//            IsEnabled = request.IsEnabled,
//            Notes = request.Notes?.Trim()
//        };

//        dbContext.ApiEnvironments.Add(entity);
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return MapEnvironment(entity);
//    }

//    public async Task<EnvironmentResponse> UpdateEnvironmentAsync(
//        Guid apiId,
//        Guid versionId,
//        Guid environmentId,
//        CreateEnvironmentRequest request,
//        CancellationToken cancellationToken)
//    {
//        var entity = await dbContext.ApiEnvironments.Include(x => x.Secrets).SingleOrDefaultAsync(
//            x => x.Id == environmentId && x.ApiVersionId == versionId && x.ApiVersion.ApiAssetId == apiId,
//            cancellationToken)
//            ?? throw new NotFoundException("API environment was not found.");

//        if (!Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out var uri) ||
//            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
//            throw new RequestValidationException("BaseUrl must be an absolute HTTP or HTTPS URL.");
//        var duplicate = await dbContext.ApiEnvironments.AnyAsync(
//            x => x.Id != environmentId && x.ApiVersionId == versionId && x.EnvironmentType == request.EnvironmentType,
//            cancellationToken);
//        if (duplicate)
//            throw new ConflictException("This environment is already registered for the API version.");

//        entity.EnvironmentType = request.EnvironmentType;
//        entity.BaseUrl = request.BaseUrl.TrimEnd('/');
//        entity.IsEnabled = request.IsEnabled;
//        entity.Notes = request.Notes?.Trim();
//        await dbContext.SaveChangesAsync(cancellationToken);
//        return MapEnvironment(entity);
//    }

//    public async Task<EnvironmentResponse> SetEnvironmentSecretAsync(
//        Guid apiId,
//        Guid versionId,
//        Guid environmentId,
//        SetEnvironmentSecretRequest request,
//        CancellationToken cancellationToken)
//    {
//        var environment = await dbContext.ApiEnvironments
//            .Include(x => x.Secrets)
//            .SingleOrDefaultAsync(
//                x => x.Id == environmentId && x.ApiVersionId == versionId && x.ApiVersion.ApiAssetId == apiId,
//                cancellationToken)
//            ?? throw new NotFoundException("API environment was not found.");

//        var name = request.Name.Trim().ToUpperInvariant();
//        var existing = environment.Secrets.SingleOrDefault(
//            x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

//        if (existing is null)
//        {
//            existing = new EnvironmentSecret
//            {
//                ApiEnvironmentId = environmentId,
//                Name = name,
//                EncryptedValue = secretProtector.Protect(request.Value)
//            };
//            dbContext.EnvironmentSecrets.Add(existing);
//        }
//        else
//        {
//            existing.EncryptedValue = secretProtector.Protect(request.Value);
//        }

//        await dbContext.SaveChangesAsync(cancellationToken);
//        return await GetEnvironmentAsync(environmentId, cancellationToken);
//    }

//    public async Task<ApiVersionResponse> GetVersionAsync(Guid versionId, CancellationToken cancellationToken)
//    {
//        var entity = await dbContext.ApiVersions.AsNoTracking()
//            .Include(x => x.Endpoints)
//            .Include(x => x.Environments)
//                .ThenInclude(x => x.Secrets)
//            .SingleOrDefaultAsync(x => x.Id == versionId, cancellationToken)
//            ?? throw new NotFoundException("API version was not found.");
//        return MapVersion(entity);
//    }

//    private async Task<EnvironmentResponse> GetEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken)
//    {
//        var environment = await dbContext.ApiEnvironments.AsNoTracking()
//            .Include(x => x.Secrets)
//            .SingleAsync(x => x.Id == environmentId, cancellationToken);
//        return MapEnvironment(environment);
//    }

//    private async Task EnsureLookupValuesExistAsync(Guid businessAreaId, Guid teamId, CancellationToken cancellationToken)
//    {
//        if (!await dbContext.BusinessAreas.AnyAsync(x => x.Id == businessAreaId, cancellationToken))
//            throw new RequestValidationException("Business area does not exist.");
//        if (!await dbContext.DevelopmentTeams.AnyAsync(x => x.Id == teamId, cancellationToken))
//            throw new RequestValidationException("Development team does not exist.");
//    }

//    private async Task EnsureVersionBelongsToApiAsync(Guid apiId, Guid versionId, CancellationToken cancellationToken)
//    {
//        if (!await dbContext.ApiVersions.AnyAsync(x => x.Id == versionId && x.ApiAssetId == apiId, cancellationToken))
//            throw new NotFoundException("API version was not found.");
//    }

//    private static void ValidateJson(string? value, string fieldName)
//    {
//        if (string.IsNullOrWhiteSpace(value)) return;
//        try { JsonDocument.Parse(value); }
//        catch (JsonException ex) { throw new RequestValidationException($"{fieldName} must contain valid JSON: {ex.Message}"); }
//    }

//    private static bool IsValidTransition(ApiLifecycleStatus current, ApiLifecycleStatus next) =>
//        current == next || (current, next) switch
//        {
//            (ApiLifecycleStatus.Draft, ApiLifecycleStatus.Active) => true,
//            (ApiLifecycleStatus.Draft, ApiLifecycleStatus.Retired) => true,
//            (ApiLifecycleStatus.Active, ApiLifecycleStatus.Deprecated) => true,
//            (ApiLifecycleStatus.Active, ApiLifecycleStatus.Retired) => true,
//            (ApiLifecycleStatus.Deprecated, ApiLifecycleStatus.Retired) => true,
//            _ => false
//        };

//    private static void ApplyLifecycleDates(ApiVersion entity, ApiLifecycleStatus status)
//    {
//        var now = DateTime.UtcNow;
//        if (status == ApiLifecycleStatus.Active)
//            entity.ReleaseDateUtc ??= now;
//        if (status == ApiLifecycleStatus.Deprecated)
//            entity.DeprecatedAtUtc ??= now;
//        if (status == ApiLifecycleStatus.Retired)
//            entity.RetiredAtUtc ??= now;
//    }

//    private static ApiDetailResponse MapApiDetail(ApiAsset entity) => new()
//    {
//        Id = entity.Id,
//        Name = entity.Name,
//        ApiProjectName = entity.ApiProjectName,
//        Description = entity.Description,
//        OwnershipType = entity.OwnershipType,
//        Protocol = entity.Protocol,
//        CreatorName = entity.CreatorName,
//        CreatorEmail = entity.CreatorEmail,
//        VendorName = entity.VendorName,
//        ExternalReferenceUrl = entity.ExternalReferenceUrl,
//        BusinessArea = new LookupResponse { Id = entity.BusinessArea.Id, Code = entity.BusinessArea.Code, Name = entity.BusinessArea.Name },
//        DevelopmentTeam = new LookupResponse { Id = entity.DevelopmentTeam.Id, Code = entity.DevelopmentTeam.Code, Name = entity.DevelopmentTeam.Name },
//        Versions = entity.Versions.OrderByDescending(x => x.IsCurrent).ThenByDescending(x => x.CreatedAtUtc).Select(MapVersion).ToList()
//    };

//    private static ApiVersionResponse MapVersion(ApiVersion entity) => new()
//    {
//        Id = entity.Id,
//        Version = entity.Version,
//        ReleaseName = entity.ReleaseName,
//        LifecycleStatus = entity.LifecycleStatus,
//        ReleaseDateUtc = entity.ReleaseDateUtc,
//        DeprecatedAtUtc = entity.DeprecatedAtUtc,
//        RetiredAtUtc = entity.RetiredAtUtc,
//        ChangeLog = entity.ChangeLog,
//        AuthenticationType = entity.AuthenticationType,
//        AuthenticationInstructions = entity.AuthenticationInstructions,
//        AuthenticationConfigJson = entity.AuthenticationConfigJson,
//        MaxRequestBytes = entity.MaxRequestBytes,
//        MaxResponseBytes = entity.MaxResponseBytes,
//        TimeoutSeconds = entity.TimeoutSeconds,
//        IsCurrent = entity.IsCurrent,
//        Endpoints = entity.Endpoints.OrderBy(x => x.RelativePath).ThenBy(x => x.HttpMethod).Select(MapEndpoint).ToList(),
//        Environments = entity.Environments.OrderBy(x => x.EnvironmentType).Select(MapEnvironment).ToList()
//    };

//    private static EndpointResponse MapEndpoint(ApiEndpoint entity) => new()
//    {
//        Id = entity.Id,
//        Name = entity.Name,
//        RelativePath = entity.RelativePath,
//        HttpMethod = entity.HttpMethod,
//        Description = entity.Description,
//        RequestHeadersJson = entity.RequestHeadersJson,
//        QueryParametersJson = entity.QueryParametersJson,
//        PathParametersJson = entity.PathParametersJson,
//        RequestPayloadSample = entity.RequestPayloadSample,
//        ResponseHeadersSampleJson = entity.ResponseHeadersSampleJson,
//        ResponseBodySample = entity.ResponseBodySample,
//        SuccessStatusCodesJson = entity.SuccessStatusCodesJson,
//        SoapAction = entity.SoapAction
//    };

//    private static EnvironmentResponse MapEnvironment(ApiEnvironment entity) => new()
//    {
//        Id = entity.Id,
//        EnvironmentType = entity.EnvironmentType,
//        BaseUrl = entity.BaseUrl,
//        IsEnabled = entity.IsEnabled,
//        Notes = entity.Notes,
//        SecretNames = entity.Secrets.OrderBy(x => x.Name).Select(x => x.Name).ToList()
//    };
//}
