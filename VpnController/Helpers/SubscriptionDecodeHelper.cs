using System.Text;

namespace VpnController.Helpers;

internal static class SubscriptionDecodeHelper
{
    public static string[] DecodeSubscriptionLines(string base64Body)
    {
        var normalized = base64Body
            .Replace("\r", "", StringComparison.Ordinal)
            .Replace("\n", "", StringComparison.Ordinal)
            .Trim();

        var bytes = Convert.FromBase64String(normalized);
        var text = Encoding.UTF8.GetString(bytes);

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
