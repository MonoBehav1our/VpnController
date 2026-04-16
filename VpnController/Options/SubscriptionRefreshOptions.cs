namespace VpnController.Services;

public sealed class SubscriptionRefreshOptions
{
    /// <summary>
    /// Base URL ending with slash (настраивается в конфиге или через env).
    /// </summary>
    public string SubscriptionUrl { get; set; } = "";

    /// <summary>
    /// Minimum allowed interval between refresh cycles (6 hours). Configured values below this are raised to this.
    /// </summary>
    public const int MinimumRefreshIntervalSeconds = 6 * 60 * 60;

    /// <summary>
    /// How often to re-fetch the subscription (default: once per day).
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 24 * 60 * 60;
}
