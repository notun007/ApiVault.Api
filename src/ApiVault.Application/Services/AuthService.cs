using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using ApiVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class AuthService(IApplicationDbContext dbContext, IPasswordHasher passwordHasher, ITokenService tokenService)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        LoginResponse objLoginResponse = new LoginResponse();

        try
        {
            var normalized = request.Username.Trim().ToUpperInvariant();
            var user = await dbContext.AppUsers.SingleOrDefaultAsync(
                x => x.Username.ToUpper() == normalized, cancellationToken);


            #region New
            if (user is null)
            {
                objLoginResponse = new LoginResponse
                {
                    IsSuccess = false,
                    Message = "Invalid username or password."
                };
            }

            if (user!= null && !user.IsActive)
            {
                objLoginResponse = new LoginResponse
                {
                    IsSuccess = false,
                    Message = "Your account is inactive. Please contact the administrator."
                };
            }

            bool result = false;
            if (user != null)
            {
                result = passwordHasher.Verify(request.Password, user.PasswordHash);
            }

            if (user != null && !result)
            {
                objLoginResponse = new LoginResponse
                {
                    IsSuccess = false,
                    Message = "Invalid username or password."
                };             
            }
            #endregion

            //Old
            //if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
            //  throw new UnauthorizedAccessException("Invalid username or password.");

            if (user != null && result)
            {
                user.LastLoginAtUtc = DateTime.UtcNow;
                var token = tokenService.CreateToken(user);
                await dbContext.SaveChangesAsync(cancellationToken);

                objLoginResponse = new LoginResponse
                {
                    AccessToken = token.AccessToken,
                    ExpiresAtUtc = token.ExpiresAtUtc,
                    DisplayName = user.DisplayName,
                    Role = user.Role,
                    IsSuccess = true,
                    Message = "Looged In Successfully."
                };
            }
        }
        catch(Exception)
        {
            return new LoginResponse
            {
                IsSuccess = false,
                Message = "Something went wrong, please try again."
            };
        }

        return objLoginResponse;

    }
}

public sealed class UserAdministrationService(IApplicationDbContext dbContext, IPasswordHasher passwordHasher)
{
    public async Task<IReadOnlyList<UserAccessResponse>> GetAccessAsync(CancellationToken cancellationToken) =>
        await dbContext.AppUsers.AsNoTracking().Include(x => x.RoleAssignments).ThenInclude(x => x.Role)
            .OrderBy(x => x.Username)
            .Select(x => new UserAccessResponse
            {
                UserId = x.Id,
                Username = x.Username,
                DisplayName = x.DisplayName,
                IsActive = x.IsActive,
                Roles = x.RoleAssignments.OrderBy(a => a.Role.Name).Select(a => new UserRoleAssignmentResponse
                {
                    RoleId = a.RoleId,
                    Code = a.Role.Code,
                    Name = a.Role.Name,
                    IsActive = a.Role.IsActive
                }).ToList()
            }).ToListAsync(cancellationToken);

    public async Task UpdateRolesAsync(Guid userId, UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.AppUsers.Include(x => x.RoleAssignments)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");
        var roleIds = request.RoleIds.Distinct().ToHashSet();
        var roles = await dbContext.SecurityRoles.Where(x => roleIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Count)
            throw new RequestValidationException("One or more selected roles are invalid or inactive.");
        if (roles.Count == 0)
            throw new RequestValidationException("At least one role must be assigned to the user.");

        dbContext.AppUserRoles.RemoveRange(user.RoleAssignments);
        dbContext.AppUserRoles.AddRange(roles.Select(role => new AppUserRole { UserId = user.Id, RoleId = role.Id }));

        var legacyRole = roles.Select(x => x.Code).Select(x => x switch
        {
            "ADMIN" => (int?)UserRole.Admin,
            "API_OWNER" => (int?)UserRole.ApiOwner,
            "TESTER" => (int?)UserRole.Tester,
            "VIEWER" => (int?)UserRole.Viewer,
            _ => null
        }).Where(x => x.HasValue).Select(x => x!.Value).OrderBy(x => x).FirstOrDefault();
        if (legacyRole != 0)
            user.Role = (UserRole)legacyRole;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.AppUsers.AsNoTracking().OrderBy(x => x.Username)
            .Select(x => new UserResponse
            {
                Id = x.Id,
                Username = x.Username,
                DisplayName = x.DisplayName,
                Email = x.Email,
                Role = x.Role,
                IsActive = x.IsActive,
                LastLoginAtUtc = x.LastLoginAtUtc
            }).ToListAsync(cancellationToken);

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        if (await dbContext.AppUsers.AnyAsync(x => x.Username.ToUpper() == username.ToUpper(), cancellationToken))
            throw new ConflictException("Username already exists.");

        var entity = new AppUser
        {
            Username = username,
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email?.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true
        };
        dbContext.AppUsers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UserResponse
        {
            Id = entity.Id,
            Username = entity.Username,
            DisplayName = entity.DisplayName,
            Email = entity.Email,
            Role = entity.Role,
            IsActive = entity.IsActive
        };
    }
}
