using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VpnController.Data;
using VpnController.Repositories;
using VpnController.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.Configure<SubscriptionRefreshOptions>(
    builder.Configuration.GetSection(nameof(SubscriptionRefreshOptions)));

builder.Services.AddSingleton<SubscriptionRepository>();

builder.Services.Configure<XrayCoreOptions>(
    builder.Configuration.GetSection(nameof(XrayCoreOptions)));

builder.Services.AddSingleton<IPostConfigureOptions<XrayCoreOptions>, XrayCoreOptionsPostConfigure>();
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(nameof(DatabaseOptions)));

builder.Services.AddSingleton<XrayConfigGenerator>();
builder.Services.AddSingleton<VlessClientSubscriptionBuilder>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = SqliteDatabaseSetup.GetConnectionString(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>());
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<UserRepository>();
builder.Services.AddHostedService<SubscriptionRefreshHostedService>();

var app = builder.Build();

DatabaseMigrator.Upgrade(app.Configuration, app.Environment);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.MapControllers();

app.Run();
