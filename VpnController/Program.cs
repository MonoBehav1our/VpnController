using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using VpnController.Data;
using VpnController.Database;
using VpnController.Helpers;
using VpnController.Options;
using VpnController.Repositories;
using VpnController.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.Configure<SotaSubscriptionRefreshOptions>(
    builder.Configuration.GetSection(nameof(SotaSubscriptionRefreshOptions)));

builder.Services.AddSingleton<SotaSubscriptionRepository>();

builder.Services.Configure<XrayCoreOptions>(
    builder.Configuration.GetSection(nameof(XrayCoreOptions)));

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(nameof(DatabaseOptions)));

builder.Services.AddScoped<XrayConfigGenerator>();
builder.Services.AddScoped<ClientSubscriptionBuilder>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = SqliteDatabaseSetupHelper.GetConnectionString(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>());
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<SotaSubscriptionOutboundsResolver>();
builder.Services.AddScoped<SotaSubscriptionRefreshService>();

var app = builder.Build();

DatabaseMigrator.Upgrade(app.Configuration, app.Environment);

app.UseForwardedHeaders();

app.MapControllers();

app.Run();
