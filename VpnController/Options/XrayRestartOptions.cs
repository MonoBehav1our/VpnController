namespace VpnController.Services;

/// <summary>
/// Запись конфига на диск и перезапуск xray по POST /api/xray/restart.
/// </summary>
public sealed class XrayRestartOptions
{
    public bool Enabled { get; set; }

    public string ConfigFilePath { get; set; } = "/usr/local/etc/xray/config.json";

    public string RestartCommand { get; set; } = "systemctl restart xray";
}
