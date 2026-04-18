using Microsoft.Extensions.Options;

namespace VpnController.Services;

/// <summary>
/// Единый источник портов (<see cref="XrayCoreOptions.InboundPortFirst"/> + индекс) и shortId для инбаундов и клиентских подписок.
/// </summary>
public sealed class XrayCoreOptionsPostConfigure : IPostConfigureOptions<XrayCoreOptions>
{
    public void PostConfigure(string? name, XrayCoreOptions options)
    {
        IReadOnlyList<string>? shortIds = null;

        if (!string.IsNullOrWhiteSpace(options.InboundShortIdsCsv))
        {
            var parts = options.InboundShortIdsCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            shortIds = parts;
        }

        if (shortIds is not null)
        {
            for (var i = 0; i < options.Inbounds.Count; i++)
            {
                var s = shortIds[i]?.Trim();
                if (string.IsNullOrEmpty(s))
                {
                    throw new InvalidOperationException(
                        $"{nameof(XrayCoreOptions)}: inbound index {i} ({options.Inbounds[i].Tag}): пустой shortId.");
                }

                options.Inbounds[i].ShortIds = new List<string> { s };
            }
        }

        for (var i = 0; i < options.Inbounds.Count; i++)
        {
            options.Inbounds[i].Port = options.InboundPortFirst + i;
        }

        for (var i = 0; i < options.Inbounds.Count; i++)
        {
            if (options.Inbounds[i].ShortIds.Count == 0 || string.IsNullOrWhiteSpace(options.Inbounds[i].ShortIds[0]))
            {
                throw new InvalidOperationException(
                    $"{nameof(XrayCoreOptions)}: inbound «{options.Inbounds[i].Tag}»: задайте shortId через {nameof(XrayCoreOptions)}:{nameof(XrayCoreOptions.InboundShortIdsCsv)} ({options.Inbounds.Count} значений через запятую) или {nameof(XrayCoreOptions)}:{nameof(XrayCoreOptions.InboundShortIds)} (массив из {options.Inbounds.Count} строк).");
            }
        }
    }
}
