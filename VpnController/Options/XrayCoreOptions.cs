namespace VpnController.Services;

/// <summary>
/// Статические параметры инбаундов и логов для генерации конфига ядра Xray.
/// Списки клиентов и upstream берутся из БД и подписки.
/// </summary>
public sealed class XrayCoreOptions
{
    public string LogLevel { get; set; } = "warning";

    /// <summary>
    /// Публичный адрес (IP или DNS), который подставляется в vless:// для клиентских подписок (HAPP и т.д.).
    /// </summary>
    public string PublicHost { get; set; } = "";

    public InboundSharedOptions InboundShared { get; set; } = new();

    /// <summary>
    /// Ровно 10 shortId Reality (по порядку инбаундов). Перекрывает ShortIds у элементов <see cref="Inbounds"/>.
    /// Альтернатива: <see cref="InboundShortIdsCsv"/>.
    /// </summary>
    public List<string> InboundShortIds { get; set; } = new();

    /// <summary>
    /// Десять shortId через запятую (тот же порядок, что и инбаунды). Удобно для одной env-переменной.
    /// </summary>
    public string? InboundShortIdsCsv { get; set; }

    public List<InboundBindingOptions> Inbounds { get; set; } = new();

    /// <summary>Первый порт инбаундов; далее +1 на каждый по порядку (<c>InboundPortFirst</c> … <c>InboundPortFirst + Inbounds.Count − 1</c>).</summary>
    public int InboundPortFirst { get; set; } = 8080;
}