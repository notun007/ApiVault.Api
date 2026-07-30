using ApiVault.Application.Abstractions;
using ApiVault.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class AuditLogService(IApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<AuditLogResponse>> GetAsync(
        string? entityType,
        string? entityId,
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 500);
        var source = dbContext.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType)) source = source.Where(x => x.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) source = source.Where(x => x.EntityId == entityId);

        return await source.OrderByDescending(x => x.OccurredAtUtc).Take(take)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                OccurredAtUtc = x.OccurredAtUtc,
                UserName = x.UserName,
                IpAddress = x.IpAddress,
                CorrelationId = x.CorrelationId,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                ChangesJson = x.ChangesJson
            }).ToListAsync(cancellationToken);
    }
}
