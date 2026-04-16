namespace VpnController.Services;

public sealed class SubscriptionRefreshOptions
{
    public const string SectionName = "Subscriptions";

    /// <summary>
    /// Base URL ending with slash (настраивается в конфиге или через env).
    /// </summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>
    /// Minimum allowed interval between refresh cycles (6 hours). Configured values below this are raised to this.
    /// </summary>
    public const int MinimumRefreshIntervalSeconds = 6 * 60 * 60;

    /// <summary>
    /// How often to re-fetch the subscription (default: once per day).
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 24 * 60 * 60;

    /// <summary>
    /// Единственный Guid подписки (как в URL /sub/{guid}). Пусто — фоновый фетч не выполняется.
    /// </summary>
    public string? SubscriptionGuid { get; set; }
}
