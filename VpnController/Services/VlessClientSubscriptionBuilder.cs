using System.Text;
using Microsoft.Extensions.Options;

namespace VpnController.Services;

/// <summary>
/// Собирает строки vless:// для клиентской подписки (формат как у SOTA: Reality, tcp, vision).
/// </summary>
public sealed class VlessClientSubscriptionBuilder
{
    private readonly XrayCoreOptions _options;

    public VlessClientSubscriptionBuilder(IOptions<XrayCoreOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// По одной строке на каждый инбаунд из конфига (порядок как в <see cref="XrayCoreOptions.Inbounds"/>).
    /// </summary>
    public string[] BuildLinesForUser(Guid userId)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicHost))
        {
            throw new InvalidOperationException(
                "Укажите Xray:PublicHost — публичный IP или DNS, куда клиенты подключаются.");
        }

        var shared = _options.InboundShared;
        if (string.IsNullOrWhiteSpace(shared.PublicKey))
        {
            throw new InvalidOperationException(
                "Укажите Xray:InboundShared:PublicKey (пара к PrivateKey, из вывода xray x25519).");
        }

        if (shared.ServerNames.Count == 0)
        {
            throw new InvalidOperationException("Xray:InboundShared:ServerNames не должен быть пустым.");
        }

        if (_options.Inbounds.Count == 0)
        {
            throw new InvalidOperationException("Xray:Inbounds пуст.");
        }

        var sni = shared.ServerNames[0];
        var uid = userId.ToString("D");
        var host = FormatHostForVlessUri(_options.PublicHost.Trim());
        var pbk = shared.PublicKey.Trim();

        var lines = new string[_options.Inbounds.Count];
        for (var i = 0; i < _options.Inbounds.Count; i++)
        {
            var ib = _options.Inbounds[i];
            var sid = ib.ShortIds.FirstOrDefault();
            if (string.IsNullOrEmpty(sid))
            {
                throw new InvalidOperationException(
                    $"Инбаунд «{ib.Tag}»: задайте хотя бы один ShortIds.");
            }

            var name = ib.Tag.EndsWith("-in", StringComparison.Ordinal)
                ? ib.Tag[..^3]
                : ib.Tag;

            lines[i] = BuildVlessLine(uid, host, ib.Port, sni, pbk, sid.Trim(), name);
        }

        return lines;
    }

    /// <summary>
    /// Тело подписки в том же виде, что отдаёт SOTA: base64(UTF-8) от списка строк, разделённых переводами строк.
    /// </summary>
    public static string ToSubscriptionBase64(IReadOnlyList<string> vlessLines)
    {
        var text = string.Join("\n", vlessLines);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>IPv6 в vless:// должен быть в квадратных скобках.</summary>
    private static string FormatHostForVlessUri(string host)
    {
        if (host.Length == 0)
        {
            return host;
        }

        if (host.Contains(':', StringComparison.Ordinal) && host[0] != '[')
        {
            return $"[{host}]";
        }

        return host;
    }

    private static string BuildVlessLine(
        string userId,
        string host,
        int port,
        string sni,
        string publicKey,
        string shortId,
        string displayName)
    {
        var sb = new StringBuilder();
        sb.Append("vless://");
        sb.Append(userId);
        sb.Append('@');
        sb.Append(host);
        sb.Append(':');
        sb.Append(port);
        sb.Append("?encryption=none&security=reality&type=tcp&flow=xtls-rprx-vision");
        sb.Append("&sni=");
        sb.Append(Uri.EscapeDataString(sni));
        sb.Append("&pbk=");
        sb.Append(Uri.EscapeDataString(publicKey));
        sb.Append("&sid=");
        sb.Append(Uri.EscapeDataString(shortId));
        sb.Append("&fp=chrome");
        sb.Append('#');
        sb.Append(Uri.EscapeDataString(displayName));
        return sb.ToString();
    }
}
