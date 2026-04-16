using Microsoft.AspNetCore.WebUtilities;

namespace VpnController.Services;

/// <summary>
/// Разбор строки подписки вида <c>vless://uuid@host:port?...</c> для маппинга в outbound.
/// </summary>
public static class VlessUriParser
{
    public static bool TryParse(string line, out ParsedVlessConnection? connection)
    {
        connection = null;
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, "vless", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var userInfo = Uri.UnescapeDataString(uri.UserInfo);
        if (string.IsNullOrEmpty(userInfo))
        {
            return false;
        }

        var host = uri.IdnHost;
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        var port = uri.Port < 0 ? 443 : uri.Port;

        var query = QueryHelpers.ParseQuery(uri.Query);
        var flow = GetQuery(query, "flow");
        var sni = GetQuery(query, "sni");
        var pbk = GetQuery(query, "pbk");
        var sid = GetQuery(query, "sid");
        var fp = GetQuery(query, "fp");
        var fragment = string.IsNullOrEmpty(uri.Fragment)
            ? null
            : Uri.UnescapeDataString(uri.Fragment);

        connection = new ParsedVlessConnection
        {
            Host = host,
            Port = port,
            UserId = userInfo,
            Flow = flow,
            ServerName = sni,
            PublicKey = pbk,
            ShortId = sid,
            Fingerprint = fp,
            Fragment = fragment
        };

        return true;
    }

    private static string? GetQuery(IReadOnlyDictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key)
    {
        foreach (var kv in query)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                var v = kv.Value.FirstOrDefault();
                return string.IsNullOrEmpty(v) ? null : v;
            }
        }

        return null;
    }
}
