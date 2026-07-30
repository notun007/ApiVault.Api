using System.Text.Json;
using ApiVault.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ApiVault.Infrastructure.Http;

public sealed class SecretRedactor(IOptions<ExecutionSecurityOptions> options)
{
    private readonly HashSet<string> _sensitiveHeaders = new(
        new[]
        {
            "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie",
            "X-Api-Key", "Api-Key", "X-Auth-Token"
        }.Concat(options.Value.AdditionalSensitiveHeaders),
        StringComparer.OrdinalIgnoreCase);

    public string SerializeHeaders(IReadOnlyDictionary<string, string> headers, IEnumerable<string> secretValues)
    {
        var redacted = headers.ToDictionary(
            x => x.Key,
            x => _sensitiveHeaders.Contains(x.Key) ? "***REDACTED***" : RedactText(x.Value, secretValues),
            StringComparer.OrdinalIgnoreCase);
        return JsonSerializer.Serialize(redacted);
    }

    public string? RedactText(string? value, IEnumerable<string> secretValues)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var result = value;
        foreach (var secret in secretValues.Where(x => !string.IsNullOrEmpty(x) && x.Length >= 4)
                     .Distinct(StringComparer.Ordinal).OrderByDescending(x => x.Length))
        {
            result = result.Replace(secret, "***REDACTED***", StringComparison.Ordinal);
        }
        return result;
    }
}
