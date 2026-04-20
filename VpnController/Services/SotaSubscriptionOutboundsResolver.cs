using VpnController.Data;
using VpnController.Helpers;

namespace VpnController.Services;

public sealed class SotaSubscriptionOutboundsResolver
{
    private readonly ILogger<SotaSubscriptionOutboundsResolver> _logger;

    public SotaSubscriptionOutboundsResolver(ILogger<SotaSubscriptionOutboundsResolver> logger)
    {
        _logger = logger ??  throw new ArgumentNullException(nameof(logger));
    }
    
    public bool TryResolve(IReadOnlyList<string> subscriptionLines, out List<SotaVlessConnection> outbounds)
    {
        outbounds = new List<SotaVlessConnection>();
        
        foreach (var subscriptionLine in subscriptionLines)
        {
            if (!VlessUriParseHelper.TryParse(subscriptionLine, out var connection) || connection is null)
            {
                _logger.LogCritical("Cannot parse sota connection: {Line}", subscriptionLine);
                throw new InvalidOperationException("SOTA changed, mapping broken");
            }

            outbounds.Add(connection);
        }

        return true;
    }
}
