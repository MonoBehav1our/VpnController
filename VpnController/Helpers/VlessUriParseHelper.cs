using Microsoft.AspNetCore.WebUtilities;
using VpnController.Data;

namespace VpnController.Helpers;

/// <summary>
/// Разбор строки подписки вида <c>vless://uuid@host:port?...</c> для маппинга в outbound.
/// </summary>
public static class VlessUriParseHelper
{
    public static bool TryParse(string line, out SotaVlessConnection? connection)
    {
        connection = null;

        if (!Uri.TryCreate(line.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var userInfo = Uri.UnescapeDataString(uri.UserInfo);
        var host = uri.IdnHost;
        var port = uri.Port;

        var query = QueryHelpers.ParseQuery(uri.Query);
        var sni = GetQuery(query, "sni");
        var pbk = GetQuery(query, "pbk");
        var sid = GetQuery(query, "sid");
        var fp = GetQuery(query, "fp");
        
        var name = Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));

        connection = new SotaVlessConnection
        {
            Name = name,
            Host = host,
            Port = port,
            UserId = userInfo,
            ServerName = sni,
            PublicKey = pbk,
            ShortId = sid,
            Fingerprint = fp,
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
