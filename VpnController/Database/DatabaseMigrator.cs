using System.Reflection;
using DbUp;
using VpnController.Helpers;

namespace VpnController.Database;

public static class DatabaseMigrator
{
    public static void Upgrade(IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = SqliteDatabaseSetupHelper.GetConnectionString(configuration, environment);

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
