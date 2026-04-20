namespace VpnController.Data;

public sealed class SotaVlessConnection
{
    public required string Name { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string UserId { get; init; }
    public required string ServerName { get; init; }
    public required string PublicKey { get; init; }
    public required string ShortId { get; init; }
    public required string Fingerprint { get; init; }
}
