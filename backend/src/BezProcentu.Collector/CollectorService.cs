using BezProcentu.Collector.Http;
using BezProcentu.Core.Stores;
using Microsoft.Extensions.Options;

namespace BezProcentu.Collector;

/// <summary>Scheduler: every tick, collects each active store whose last successful run is older than RefreshInterval.</summary>
public sealed class CollectorService(CollectionRunner runner, IOptions<CollectorOptions> options, TimeProvider time, ILogger<CollectorService> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = options.Value;
        log.LogInformation("Collector scheduler started; refresh interval {Interval}", o.RefreshInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var store in ActiveStores(o))
            {
                if (await runner.IsDueAsync(store, o.RefreshInterval, stoppingToken))
                {
                    await runner.RunStoreAsync(store, stoppingToken);
                }
            }

            await Task.Delay(o.SchedulerTick, time, stoppingToken);
        }
    }

    public static IEnumerable<string> ActiveStores(CollectorOptions o) =>
        StoreSources.All
            .Where(s => s.Status == StoreStatus.Active)
            .Select(s => s.Slug)
            .Where(s => o.Stores.Length == 0 || o.Stores.Contains(s, StringComparer.OrdinalIgnoreCase));
}
