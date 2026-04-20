using Microsoft.AspNetCore.Http;

namespace VpnController.Helpers;

internal static class BearerTokenAuthHelper
{
    private const string BearerPrefix = "Bearer ";

    public static bool IsValid(HttpRequest request, string? expectedToken)
    {
        if (string.IsNullOrEmpty(expectedToken))
            return false;

        if (!request.Headers.TryGetValue("Authorization", out var values))
            return false;

        var auth = values.FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || auth.Length <= BearerPrefix.Length
                                         || !auth.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var token = auth.AsSpan(BearerPrefix.Length).Trim();
        return token.SequenceEqual(expectedToken.AsSpan());
    }
}
