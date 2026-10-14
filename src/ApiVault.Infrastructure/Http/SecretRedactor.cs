using System.Text.Json;
using System.Text.Json.Nodes;
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

    public string RedactJsonPaths(string value, IEnumerable<string> jsonPaths)
    {
        var paths = jsonPaths.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
        if (paths.Length == 0 || string.IsNullOrWhiteSpace(value)) return value;

        try
        {
            var root = JsonNode.Parse(value);
            if (root is null) return value;

            foreach (var path in paths)
            {
                var segments = path.Trim().TrimStart('$').TrimStart('.').Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0) continue;

                JsonNode? current = root;
                for (var index = 0; index < segments.Length - 1 && current is JsonObject currentObject; index++)
                {
                    var key = currentObject.Select(x => x.Key)
                        .FirstOrDefault(x => string.Equals(x, segments[index], StringComparison.OrdinalIgnoreCase));
                    current = key is null ? null : currentObject[key];
                }

                if (current is not JsonObject parent) continue;
                var finalKey = parent.Select(x => x.Key)
                    .FirstOrDefault(x => string.Equals(x, segments[^1], StringComparison.OrdinalIgnoreCase));
                if (finalKey is not null) parent[finalKey] = "***REDACTED***";
            }

            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return value;
        }
    }
}
