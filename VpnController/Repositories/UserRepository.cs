using Microsoft.EntityFrameworkCore;
using VpnController.Data;
using VpnController.Services;

namespace VpnController.Repositories;

public sealed class UserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User> CreateAsync(string alias, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAlias(alias);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException("Укажите непустой alias.", nameof(alias));
        }

        if (await AliasExistsAsync(normalized, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Пользователь с ярлыком «{normalized}» уже существует.");
        }

        var uuids = new List<Guid>(15);
        for (var i = 0; i < 15; i++)
        {
            uuids.Add(Guid.NewGuid());
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Alias = normalized,
            ClientUuids = uuids
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Where(u => u.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false) > 0;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<User?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAlias(alias);
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Alias.ToLower() == normalized.ToLower(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Alias)
            .ThenBy(u => u.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> AliasExistsAsync(string normalizedAlias, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AnyAsync(u => u.Alias.ToLower() == normalizedAlias.ToLower(), cancellationToken)
            .ConfigureAwait(false);
    }

    private static string NormalizeAlias(string alias) => alias.Trim();
}
