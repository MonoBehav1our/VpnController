namespace VpnController.Services;

public sealed class InboundBindingOptions
{
    public string Tag { get; set; } = "";

    public int Port { get; set; }

    public List<string> ShortIds { get; set; } = new();
}