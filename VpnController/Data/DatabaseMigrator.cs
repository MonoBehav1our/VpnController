using System.Reflection;
using DbUp;
using DbUp.Sqlite;

namespace VpnController.Data;

public static class DatabaseMigrator
{
    /// <summary>
    /// Применяет SQL-скрипты из сборки (embedded <c>Database/Scripts/*.sql</c>), журнал — таблица SchemaVersions.
    /// </summary>
    public static void Upgrade(IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = SqliteDatabaseSetup.GetConnectionString(configuration, environment);

        var upgrader = DeployChanges.To
            .SqliteDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                name => name.Contains("Database.Scripts", StringComparison.Ordinal)
                        && name.EndsWith(".sql", StringComparison.Ordinal))
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw result.Error;
        }
    }
}
