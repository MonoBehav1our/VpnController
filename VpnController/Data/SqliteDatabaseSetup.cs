namespace VpnController.Data;

/// <summary>
/// Путь к файлу SQLite и строка подключения (каталог создаётся при необходимости).
/// </summary>
public static class SqliteDatabaseSetup
{
    public static string GetConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var relativePath = configuration["Database:SqlitePath"] ?? "data/vpncontroller.db";
        var fullPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativePath));
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return $"Data Source={fullPath}";
    }
}
