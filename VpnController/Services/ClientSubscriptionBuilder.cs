using Microsoft.Extensions.Options;
using VpnController.Data;
using VpnController.Options;
using VpnController.Repositories;

namespace VpnController.Services;

public sealed class ClientSubscriptionBuilder
{
    private readonly SotaSubscriptionRepository _sotaSubscriptionRepository;
    private readonly XrayCoreOptions _xrayCoreOptions;

    public ClientSubscriptionBuilder(SotaSubscriptionRepository sotaSubscriptionRepository, IOptions<XrayCoreOptions> options)
    {
        _sotaSubscriptionRepository = sotaSubscriptionRepository ?? throw new ArgumentNullException(nameof(sotaSubscriptionRepository));
        _xrayCoreOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
    }
    
    public List<string> BuildLinesForUser(User user)
    {
        var lines = new List<string>();

        lines.AddRange(BuildSotaLines());
        lines.AddRange(BuildUserLines(user));

        return lines;
    }
    
    private IEnumerable<string> BuildSotaLines()
    {
        var sotaConnections = _sotaSubscriptionRepository.GetConnections();

        foreach (var c in sotaConnections)
        {
            yield return BuildVlessLine(
                c.UserId, c.Host, c.Port,
                c.ServerName, c.PublicKey, 
                c.ShortId, c.Fingerprint, c.Name);
        }
    }
    
    private IEnumerable<string> BuildUserLines(User user)
    {
        var sni = _xrayCoreOptions.ServerName;
        var host = _xrayCoreOptions.PublicHost;
        var pbk = _xrayCoreOptions.PublicKey;
        var port = _xrayCoreOptions.InboundPort;
        var shortId = _xrayCoreOptions.ShortId;
        var fingerprint = _xrayCoreOptions.Fingerprint;

        for (var i = 0; i < user.ClientUuids.Count; i++)
        {
            yield return BuildVlessLine(
                user.ClientUuids[i].ToString(),
                host, port, sni, pbk, shortId, fingerprint,
                "Обход: " + _sotaSubscriptionRepository.GetConnection(i).Name
            );
        }
    }
    

    private static string BuildVlessLine(
        string userId,
        string host,
        int port,
        string sni,
        string publicKey,
        string shortId,
        string fingerprint,
        string displayName)
    {
        
        var sb = new System.Text.StringBuilder();
        sb.Append("vless://");
        sb.Append(userId);
        sb.Append('@');
        sb.Append(host);
        sb.Append(':');
        sb.Append(port);
        sb.Append("?security=reality&type=tcp&flow=xtls-rprx-vision");
        sb.Append("&sni=");
        sb.Append(Uri.EscapeDataString(sni));
        sb.Append("&pbk=");
        sb.Append(Uri.EscapeDataString(publicKey));
        sb.Append("&sid=");
        sb.Append(Uri.EscapeDataString(shortId));
        sb.Append("&fp=");
        sb.Append(Uri.EscapeDataString(fingerprint));
        sb.Append('#');
        sb.Append(Uri.EscapeDataString(displayName));
        
        return sb.ToString();
    }
}
