using VpnController.Database;
using VpnController.Options;

namespace VpnController.Helpers;

public static class SqliteDatabaseSetupHelper
{
    public static string GetConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var relativePath = configuration.GetSection(nameof(DatabaseOptions)).GetValue<string>(nameof(DatabaseOptions.SqlitePath))
            ?? "data/vpncontroller.db";
        var fullPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativePath));
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return $"Data Source={fullPath}";
    }
}
