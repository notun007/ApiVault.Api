using System.Net;
using System.Net.Sockets;
using ApiVault.Application.Common;
using ApiVault.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ApiVault.Infrastructure.Http;

public sealed class SsrfGuard(IOptions<ExecutionSecurityOptions> options)
{
    private static readonly HashSet<string> MetadataHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "metadata.google.internal",
        "metadata.azure.internal",
        "instance-data.ec2.internal"
    };

    public async Task ValidateAsync(Uri uri, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!uri.IsAbsoluteUri)
            throw new RequestValidationException("The target URL must be absolute.");
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            throw new RequestValidationException("Only HTTP and HTTPS targets are allowed.");
        if (uri.Scheme == Uri.UriSchemeHttp && !settings.AllowHttp)
            throw new RequestValidationException("Plain HTTP execution is disabled.");
        if (!string.IsNullOrEmpty(uri.UserInfo))
            throw new RequestValidationException("Credentials must not be embedded in the URL.");

        var host = uri.IdnHost.TrimEnd('.');
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || MetadataHosts.Contains(host))
            throw new RequestValidationException("The target host is blocked by the SSRF policy.");

        IPAddress[] addresses;
        if (IPAddress.TryParse(host, out var literalAddress))
            addresses = [literalAddress];
        else
        {
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            }
            catch (SocketException ex)
            {
                throw new RequestValidationException($"The target host could not be resolved: {ex.Message}");
            }
        }

        if (addresses.Length == 0)
            throw new RequestValidationException("The target host did not resolve to an IP address.");

        foreach (var address in addresses)
        {
            if (IsCloudMetadataAddress(address))
                throw new RequestValidationException("Cloud metadata destinations are blocked.");
        }

        if (IsAllowedHost(host, settings.AllowedHosts))
            return;

        if (!settings.AllowPrivateNetworks && addresses.Any(IsPrivateOrReserved))
            throw new RequestValidationException(
                "The target resolves to a private, loopback, link-local, multicast, or reserved address. " +
                "Add the exact approved host to ApiExecutionSecurity:AllowedHosts to permit a bank-internal destination.");
    }

    private static bool IsAllowedHost(string host, IEnumerable<string> patterns)
    {
        foreach (var rawPattern in patterns)
        {
            var pattern = rawPattern.Trim().TrimEnd('.');
            if (string.IsNullOrWhiteSpace(pattern)) continue;
            if (string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase)) return true;
            if (pattern.StartsWith("*.", StringComparison.Ordinal) &&
                host.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase) &&
                host.Length > pattern.Length - 1)
                return true;
        }
        return false;
    }

    private static bool IsCloudMetadataAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = address.GetAddressBytes();
        return b[0] == 169 && b[1] == 254 && b[2] == 169 && b[3] == 254;
    }

    private static bool IsPrivateOrReserved(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return true;

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == 0 ||
                   bytes[0] == 10 ||
                   (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) ||
                   bytes[0] == 127 ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   bytes[0] >= 224;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal ||
                   address.IsIPv6Multicast ||
                   (bytes[0] & 0xFE) == 0xFC;
        }

        return true;
    }
}
