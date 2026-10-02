using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController, Authorize]
[Route("api/vendors")]
public sealed class VendorsController(VendorService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorResponse>>> GetAll([FromQuery] bool activeOnly = false, CancellationToken ct = default) =>
        Ok(await service.GetAllAsync(activeOnly, ct));

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<VendorResponse>> Create(SaveVendorRequest request, CancellationToken ct) =>
        Ok(await service.SaveAsync(null, request, ct));

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<VendorResponse>> Update(Guid id, SaveVendorRequest request, CancellationToken ct) =>
        Ok(await service.SaveAsync(id, request, ct));
}
