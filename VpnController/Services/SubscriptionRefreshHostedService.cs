using Microsoft.Extensions.Options;

namespace VpnController.Services;

/// <summary>
/// Периодически скачивает подписку по <see cref="SubscriptionRefreshOptions.SubscriptionUrl"/> и обновляет <see cref="SubscriptionRepository"/>.
/// </summary>
public sealed class SubscriptionRefreshHostedService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SubscriptionRepository _repository;
    private readonly IOptions<SubscriptionRefreshOptions> _options;
    private readonly ILogger<SubscriptionRefreshHostedService> _logger;

    public SubscriptionRefreshHostedService(
        IHttpClientFactory httpClientFactory,
        SubscriptionRepository repository,
        IOptions<SubscriptionRefreshOptions> options,
        ILogger<SubscriptionRefreshHostedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _repository = repository;
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

    private async Task RefreshAsync(SubscriptionRefreshOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.SubscriptionUrl))
        {
            _logger.LogDebug("{Path} is not set; skipping refresh",
                $"{nameof(SubscriptionRefreshOptions)}:{nameof(SubscriptionRefreshOptions.SubscriptionUrl)}");
            return;
        }
        
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VpnController/1.0");
        
        try
        {
            var body = await client.GetStringAsync(new Uri(options.SubscriptionUrl), cancellationToken);
            var lines = SubscriptionDecoder.DecodeSubscriptionLines(body);
            _repository.Replace(lines);
            
            _logger.LogInformation("Updated subscription with {Count} connection strings", lines.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh subscription {url}", options.SubscriptionUrl);
        }
    }
}
