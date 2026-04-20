using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using VpnController.Helpers;
using VpnController.Options;
using VpnController.Repositories;

namespace VpnController.Services;

public sealed class SotaSubscriptionRefreshService 
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SotaSubscriptionRepository _repository;
    private readonly SotaSubscriptionOutboundsResolver _sotaSubscriptionOutboundsResolver;
    private readonly IOptions<SotaSubscriptionRefreshOptions> _options;
    private readonly ILogger<SotaSubscriptionRefreshService> _logger;

    private readonly DockerClient _dockerClient;

    public SotaSubscriptionRefreshService(
        IHttpClientFactory httpClientFactory,
        SotaSubscriptionRepository repository,
        SotaSubscriptionOutboundsResolver sotaSubscriptionOutboundsResolver,
        IOptions<SotaSubscriptionRefreshOptions> options,
        ILogger<SotaSubscriptionRefreshService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _sotaSubscriptionOutboundsResolver = sotaSubscriptionOutboundsResolver ?? throw new ArgumentNullException(nameof(sotaSubscriptionOutboundsResolver));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _dockerClient = new DockerClientConfiguration(
            new Uri("unix:///var/run/docker.sock"))
            .CreateClient();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.SubscriptionUrl))
        {
            _logger.LogCritical("{Path} is not set; skipping refresh",
                $"{nameof(SotaSubscriptionRefreshOptions)}:{nameof(SotaSubscriptionRefreshOptions.SubscriptionUrl)}");
            return;
        }
        
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VpnController/1.0");
        
        try
        {
            var body = await client.GetStringAsync(new Uri(_options.Value.SubscriptionUrl), cancellationToken);
            var lines = SubscriptionDecodeHelper.DecodeSubscriptionLines(body);

            if (!_sotaSubscriptionOutboundsResolver.TryResolve(lines, out var sotaOutbounds))
            {
                _logger.LogCritical(
                    "Subscription format changed. Provider broke contract. Lines: {Count}",
                    lines.Length);
                return;
            }

            _repository.Replace(sotaOutbounds);

            try
            {
                var configPath = Path.Combine("/xray/config", "config.json");
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath, cancellationToken);
                    JsonDocument.Parse(json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Config validation failed. Skipping Xray reload");
                return;
            }

            await ReloadXrayAsync(_options.Value.XrayContainerName, cancellationToken);

            _logger.LogInformation("Updated subscription with {Count} connection strings", lines.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh subscription {url}", _options.Value.SubscriptionUrl);
        }
    }

    private async Task ReloadXrayAsync(string containerName, CancellationToken cancellationToken)
    {
        try
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true },
                cancellationToken);

            var container = containers.FirstOrDefault(c =>
                c.Names.Any(n => n.Contains(containerName, StringComparison.OrdinalIgnoreCase)));

            if (container == null)
            {
                _logger.LogError("Xray container '{Name}' not found", containerName);
                return;
            }

            await _dockerClient.Containers.KillContainerAsync(
                container.ID,
                new ContainerKillParameters
                {
                    Signal = "HUP"
                },
                cancellationToken);

            _logger.LogInformation("Sent HUP signal to Xray container '{Name}'", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload Xray container");
        }
    }
}