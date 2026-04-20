namespace VpnController.Options;

public sealed class XrayCoreOptions
{
    public required string LogLevel { get; set; }
    public required string PublicHost { get; set; }
    public required string Dest { get; set; }
    public required string ServerName { get; set; }
    public required string PrivateKey { get; set; }
    public required string PublicKey { get; set; }
    public required string MainInboundTag { get; set; }
    public required int InboundPort { get; set; }
    public required string ShortId { get; set; }
    public required string Fingerprint { get; set; }
}
