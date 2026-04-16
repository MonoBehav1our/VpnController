using Microsoft.Extensions.Options;

namespace VpnController.Services;

/// <summary>
/// Периодически скачивает подписку по <see cref="SubscriptionRefreshOptions.SubscriptionGuid"/> и обновляет <see cref="InMemorySubscriptionStore"/>.
/// </summary>
public sealed class SubscriptionRefreshHostedService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InMemorySubscriptionStore _store;
    private readonly IOptions<SubscriptionRefreshOptions> _options;
    private readonly ILogger<SubscriptionRefreshHostedService> _logger;

    public SubscriptionRefreshHostedService(
        IHttpClientFactory httpClientFactory,
        InMemorySubscriptionStore store,
        IOptions<SubscriptionRefreshOptions> options,
        ILogger<SubscriptionRefreshHostedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _store = store;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = _options.Value;
        var seconds = Math.Max(SubscriptionRefreshOptions.MinimumRefreshIntervalSeconds, opt.RefreshIntervalSeconds);
        var interval = TimeSpan.FromSeconds(seconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshAsync(opt, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription refresh cycle failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RefreshAsync(SubscriptionRefreshOptions opt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(opt.SubscriptionGuid))
        {
            _logger.LogDebug("Subscriptions:SubscriptionGuid is not set; skipping refresh");
            return;
        }

        if (!Guid.TryParse(opt.SubscriptionGuid.Trim(), out var subscriptionId))
        {
            _logger.LogWarning("Subscriptions:SubscriptionGuid is not a valid Guid: {Value}", opt.SubscriptionGuid);
            return;
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VpnController/1.0");

        var baseUrl = opt.BaseUrl.TrimEnd('/') + "/";
        var url = $"{baseUrl}{subscriptionId:D}";

        try
        {
            var body = await client.GetStringAsync(new Uri(url), cancellationToken);
            var lines = SubscriptionDecoder.DecodeSubscriptionLines(body);
            _store.Replace(lines);
            _logger.LogInformation(
                "Updated subscription {Guid} with {Count} connection strings",
                subscriptionId,
                lines.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh subscription {Guid}", subscriptionId);
        }
    }
}
