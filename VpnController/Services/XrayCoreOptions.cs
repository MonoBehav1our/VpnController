namespace VpnController.Services;

/// <summary>
/// Статические параметры инбаундов и логов для генерации конфига ядра Xray.
/// Списки клиентов и upstream берутся из БД и подписки.
/// </summary>
public sealed class XrayCoreOptions
{
    public const string SectionName = "Xray";

    /// <summary>Ожидаемое число инбаундов: direct-in + sota-01-in … sota-09-in.</summary>
    public const int ExpectedInboundCount = 10;

    public string LogLevel { get; set; } = "warning";

    /// <summary>
    /// Публичный адрес (IP или DNS), который подставляется в vless:// для клиентских подписок (HAPP и т.д.).
    /// </summary>
    public string PublicHost { get; set; } = "";

    public InboundSharedOptions InboundShared { get; set; } = new();

    public List<InboundBindingOptions> Inbounds { get; set; } = new();
}

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

public sealed class InboundBindingOptions
{
    public string Tag { get; set; } = "";

    public int Port { get; set; }

    public List<string> ShortIds { get; set; } = new();
}
