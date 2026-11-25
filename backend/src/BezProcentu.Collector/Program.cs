using BezProcentu.Collector;
using BezProcentu.Collector.Collectors;
using BezProcentu.Collector.Http;
using BezProcentu.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddJsonConsole(o =>
{
    o.IncludeScopes = true;
    o.UseUtcTimestamp = true;
    o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<OfferWriter>();

builder.Services.AddOptions<CollectorOptions>()
    .Bind(builder.Configuration.GetSection(CollectorOptions.Section))
    .Validate(o => o.RefreshInterval > TimeSpan.Zero && o.RefreshInterval <= TimeSpan.FromHours(24), "RefreshInterval must be between 0 and 24h (data must refresh at least daily).")
    .Validate(o => o.RequestDelay >= TimeSpan.FromMilliseconds(500), "RequestDelay must be at least 500 ms.")
    .Validate(o => o.MaxPagesPerListing is > 0 and <= 100, "MaxPagesPerListing must be 1..100.")
    .ValidateOnStart();

builder.Services.AddHttpClient<PoliteFetcher>(c => c.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    });

builder.Services.AddScoped<IStoreCollector, EuronicsCollector>();
builder.Services.AddScoped<IStoreCollector, M79Collector>();
builder.Services.AddScoped<IStoreCollector, TetCollector>();
builder.Services.AddSingleton<CollectionRunner>();

var runOnce = args.Contains("--once");
if (!runOnce)
{
    builder.Services.AddHostedService<CollectorService>();
}

var host = builder.Build();

if (builder.Configuration.GetValue("Database:ApplyMigrations", false))
{
    await using var scope = host.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (args.Contains("--migrate-only"))
{
    return 0;
}

if (runOnce)
{
    // Manual run: collect every (configured) active store once and exit. Non-zero exit if any store failed.
    var runner = host.Services.GetRequiredService<CollectionRunner>();
    var opts = host.Services.GetRequiredService<IOptions<CollectorOptions>>().Value;
    var failed = 0;
    foreach (var store in CollectorService.ActiveStores(opts))
    {
        if (await runner.RunStoreAsync(store, CancellationToken.None) == RunStatus.Failed)
        {
            failed++;
        }
    }

    return failed == 0 ? 0 : 2;
}

await host.RunAsync();
return 0;
