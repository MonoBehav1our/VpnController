namespace VpnController.Data;

public sealed class User
{
    public Guid Id { get; set; }

    public string Alias { get; set; }

    public List<Guid> ClientUuids { get; set; }
}
