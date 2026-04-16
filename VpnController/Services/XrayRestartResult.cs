namespace VpnController.Services;

public sealed record XrayRestartResult(bool Ok, int StatusCode, string? Detail);
