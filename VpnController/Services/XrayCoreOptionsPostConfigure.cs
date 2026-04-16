using Microsoft.Extensions.Options;

namespace VpnController.Services;

/// <summary>
/// Единый источник портов (8080…8089) и shortId для инбаундов и клиентских подписок.
/// </summary>
public sealed class XrayCoreOptionsPostConfigure : IPostConfigureOptions<XrayCoreOptions>
{
    public void PostConfigure(string? name, XrayCoreOptions options)
    {
        if (options.Inbounds.Count != XrayCoreOptions.ExpectedInboundCount)
        {
            return;
        }

        IReadOnlyList<string>? shortIds = null;

        if (!string.IsNullOrWhiteSpace(options.InboundShortIdsCsv))
        {
            var parts = options.InboundShortIdsCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != XrayCoreOptions.ExpectedInboundCount)
            {
                throw new InvalidOperationException(
                    $"Xray:InboundShortIdsCsv must contain exactly {XrayCoreOptions.ExpectedInboundCount} comma-separated shortIds.");
            }

            shortIds = parts;
        }
        else if (options.InboundShortIds.Count == XrayCoreOptions.ExpectedInboundCount)
        {
            shortIds = options.InboundShortIds;
        }

        if (shortIds is not null)
        {
            for (var i = 0; i < options.Inbounds.Count; i++)
            {
                var s = shortIds[i]?.Trim();
                if (string.IsNullOrEmpty(s))
                {
                    throw new InvalidOperationException(
                        $"Xray: inbound index {i} ({options.Inbounds[i].Tag}): пустой shortId.");
                }

                options.Inbounds[i].ShortIds = new List<string> { s };
            }
        }

        for (var i = 0; i < options.Inbounds.Count; i++)
        {
            options.Inbounds[i].Port = XrayCoreOptions.InboundPortBase + i;
        }

        for (var i = 0; i < options.Inbounds.Count; i++)
        {
            if (options.Inbounds[i].ShortIds.Count == 0 || string.IsNullOrWhiteSpace(options.Inbounds[i].ShortIds[0]))
            {
                throw new InvalidOperationException(
                    $"Xray: inbound «{options.Inbounds[i].Tag}»: задайте shortId через Xray:InboundShortIdsCsv (10 значений через запятую) или Xray:InboundShortIds (массив из 10 строк).");
            }
        }
    }
}
