using VpnController.Data;

namespace VpnController.Repositories;

public sealed class SotaSubscriptionRepository
{
    private readonly Lock _lock;
    private List<SotaVlessConnection> _sotaVlessConnections;

    public SotaSubscriptionRepository()
    {
        _sotaVlessConnections = new List<SotaVlessConnection>();
        _lock = new Lock();
    }
    
    public IReadOnlyList<SotaVlessConnection> GetConnections()
    {
        lock (_lock)
        {
            return _sotaVlessConnections.AsReadOnly();
        }
    }

    public SotaVlessConnection? GetConnection(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _sotaVlessConnections.Count)
            {
                return null;
            }
            
            return _sotaVlessConnections[index];
        }
    }

    public void Replace(List<SotaVlessConnection> connections)
    {
        lock (_lock)
        {
            _sotaVlessConnections = connections.ToList();
        }
    }
}
