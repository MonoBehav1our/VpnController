using System.Diagnostics.CodeAnalysis;

namespace VpnController.Services;

/// <summary>
/// Одним шагом превращает первые 9 строк подписки в 9 распарсенных upstream для outbounds sota-01..sota-09.
/// </summary>
public static class SubscriptionSotaOutboundsResolver
{
    public const int RequiredLineCount = 9;

    public static bool TryResolve(
        IReadOnlyList<string> subscriptionLines,
        [NotNullWhen(true)] out ParsedVlessConnection[]? outbounds,
        out string? errorMessage)
    {
        outbounds = null;
        errorMessage = null;

        if (subscriptionLines.Count < RequiredLineCount)
        {
            errorMessage =
                $"В подписке должно быть не менее {RequiredLineCount} vless-строк для sota-01..sota-09.";
            return false;
        }

        var parsed = new ParsedVlessConnection[RequiredLineCount];
        for (var i = 0; i < RequiredLineCount; i++)
        {
            if (!VlessUriParser.TryParse(subscriptionLines[i], out var conn) || conn is null)
            {
                errorMessage = $"Некорректная vless-строка (строка {i + 1}): {subscriptionLines[i]}";
                return false;
            }

            parsed[i] = conn;
        }

        outbounds = parsed;
        return true;
    }
}
