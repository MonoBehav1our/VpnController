using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace VpnController.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        JsonSerializerOptions jsonOptions = new();
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Alias)
                .HasColumnName("Alias")
                .HasDefaultValue("");
            
            entity.Property(e => e.ClientUuids)
                .HasColumnName("ClientUuidsJson")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrEmpty(v) ? new List<Guid>() 
                        : JsonSerializer.Deserialize<List<Guid>>(v, jsonOptions) ?? new List<Guid>());
            
            entity.HasIndex(e => e.Alias)
                .IsUnique()
                .HasFilter("Alias <> ''");
        });
    }
}
