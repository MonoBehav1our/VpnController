namespace VpnController.Services;

/// <summary>
/// Поля из vless:// URI (подписка) для сборки outbound Xray.
/// </summary>
public sealed class ParsedVlessConnection
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string UserId { get; init; }
    public string? Flow { get; init; }
    public string? ServerName { get; init; }
    public string? PublicKey { get; init; }
    public string? ShortId { get; init; }
    public string? Fingerprint { get; init; }
    public string? Fragment { get; init; }
}
