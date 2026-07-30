using ApiVault.Domain.Entities;

namespace ApiVault.Application.Abstractions;

public interface IUserContext
{
    string UserName { get; }
    string? IpAddress { get; }
    string? CorrelationId { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    TokenResult CreateToken(AppUser user);
}

public sealed class TokenResult
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}

public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
