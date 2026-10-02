using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class VendorService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<VendorResponse>> GetAllAsync(bool activeOnly, CancellationToken ct)
    {
        var query = db.Vendors.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).Select(x => new VendorResponse
        {
            Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description,
            ContactPerson = x.ContactPerson, SupportEmail = x.SupportEmail, SupportPhone = x.SupportPhone,
            WebsiteUrl = x.WebsiteUrl, IsActive = x.IsActive, SystemCount = x.Systems.Count
        }).ToListAsync(ct);
    }

    public async Task<VendorResponse> SaveAsync(Guid? id, SaveVendorRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new RequestValidationException("Vendor code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new RequestValidationException("Vendor name is required.");
        if (await db.Vendors.AnyAsync(x => x.Id != id && (x.Code.ToUpper() == code || x.Name.ToUpper() == name.ToUpper()), ct))
            throw new ConflictException("A vendor with the same code or name already exists.");
        var entity = id.HasValue
            ? await db.Vendors.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Vendor was not found.")
            : new Vendor();
        entity.Code = code; entity.Name = name; entity.Description = request.Description?.Trim();
        entity.ContactPerson = request.ContactPerson?.Trim(); entity.SupportEmail = request.SupportEmail?.Trim();
        entity.SupportPhone = request.SupportPhone?.Trim(); entity.WebsiteUrl = request.WebsiteUrl?.Trim();
        entity.IsActive = request.IsActive;
        if (!id.HasValue) db.Vendors.Add(entity);
        await db.SaveChangesAsync(ct);
        return (await GetAllAsync(false, ct)).Single(x => x.Id == entity.Id);
    }
}
