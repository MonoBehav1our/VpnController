namespace VpnController.Services;

public sealed class InboundSharedOptions
{
    public string Dest { get; set; } = "";

    public List<string> ServerNames { get; set; } = new();

    public string PrivateKey { get; set; } = "";

    /// <summary>
    /// Публичный ключ Reality (pbk в vless), пара к <see cref="PrivateKey"/> — из вывода <c>xray x25519</c>.
    /// </summary>
    public string PublicKey { get; set; } = "";
}