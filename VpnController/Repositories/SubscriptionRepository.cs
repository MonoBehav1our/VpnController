namespace VpnController.Services;

/// <summary>
/// In-memory кэш декодированных строк подписки (одна подписка на процесс).
/// </summary>
public sealed class SubscriptionRepository
{
    private readonly object _lock = new();
    private string[]? _lines;

    public bool TryGetLines(out IReadOnlyList<string> lines)
    {
        lock (_lock)
        {
            if (_lines is null)
            {
                lines = Array.Empty<string>();
                return false;
            }

            lines = _lines;
            return true;
        }
    }

    public void Replace(string[] lines)
    {
        lock (_lock)
        {
            _lines = lines;
        }
    }
}
