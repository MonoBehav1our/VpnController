namespace VpnController.Services;

/// <summary>
/// Одним шагом превращает первые 9 строк подписки в 9 распарсенных upstream для outbounds sota-01..sota-09.
/// </summary>
public static class SubscriptionSotaOutboundsResolver
{
    public static bool TryResolve(IReadOnlyList<string> subscriptionLines, out ParsedVlessConnection[]? outbounds)
    {
        outbounds = null;
        
        var parsed = new ParsedVlessConnection[subscriptionLines.Count];
        for (var i = 0; i < subscriptionLines.Count; i++)
        {
            if (!VlessUriParser.TryParse(subscriptionLines[i], out var conn) || conn is null)
            {
                // todo: return logging
                return false;
            }

            parsed[i] = conn;
        }

        outbounds = parsed;
        return true;
    }
}
