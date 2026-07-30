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
