using System.Text.RegularExpressions;
using ApiVault.Application.Common;

namespace ApiVault.Infrastructure.Http;

public sealed partial class SecretTemplateResolver
{
    [GeneratedRegex(@"\{\{secret:([A-Za-z0-9_.-]+)\}\}", RegexOptions.IgnoreCase)]
    private static partial Regex SecretPlaceholderRegex();

    public string? Resolve(string? value, IReadOnlyDictionary<string, string> secrets)
    {
        if (value is null) return null;
        return SecretPlaceholderRegex().Replace(value, match =>
        {
            var name = match.Groups[1].Value;
            if (!secrets.TryGetValue(name, out var secret))
                throw new RequestValidationException($"Secret '{name}' is not configured for the selected environment.");
            return secret;
        });
    }
}
