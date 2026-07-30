using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApiVault.Application.Abstractions;
using ApiVault.Domain.Entities;
using ApiVault.Infrastructure.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiVault.Infrastructure.Security;

public sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
{
    public string UserName => accessor.HttpContext?.User.Identity?.Name ?? "system";
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;
}

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256") return false;
            var iterations = int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public TokenResult CreateToken(AppUser user)
    {
        var settings = options.Value;
        if (Encoding.UTF8.GetByteCount(settings.SigningKey) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(settings.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("display_name", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now, expires, credentials);
        return new TokenResult
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires
        };
    }
}

public sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("ApiVault.EnvironmentSecrets.v1");
    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
