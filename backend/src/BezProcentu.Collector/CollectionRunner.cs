using BezProcentu.Collector.Collectors;
using BezProcentu.Collector.Http;
using BezProcentu.Data;
using Microsoft.EntityFrameworkCore;

namespace BezProcentu.Collector;

/// <summary>Runs one store's collector and records the run, including failures, in collection_runs.</summary>
public sealed class CollectionRunner(IServiceScopeFactory scopes, TimeProvider time, ILogger<CollectionRunner> log)
{
    public async Task<RunStatus> RunStoreAsync(string storeSlug, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var collector = scope.ServiceProvider.GetServices<IStoreCollector>().Single(c => c.StoreSlug == storeSlug);
        var writer = scope.ServiceProvider.GetRequiredService<OfferWriter>();

        var run = new CollectionRunEntity { StoreSlug = storeSlug, StartedAt = time.GetUtcNow(), Status = RunStatus.Running };
        db.CollectionRuns.Add(run);
        await db.SaveChangesAsync(ct);

        using var _ = log.BeginScope(new Dictionary<string, object> { ["store"] = storeSlug, ["runId"] = run.Id });
        log.LogInformation("Collection started");

        try
        {
            var outcome = await collector.CollectAsync(ct);
            run.PagesFetched = outcome.PagesFetched;
            run.PagesFailed = outcome.PagesFailed;
            run.StorePolicyActive = outcome.StorePolicyActive;

            if (!outcome.StorePolicyActive && outcome.Products.Count == 0)
            {
                await writer.DeactivateStoreAsync(storeSlug, ct);
                run.OffersActive = 0;
            }
            else
            {
                run.OffersActive = await writer.SaveAsync(storeSlug, outcome.Products, outcome.Complete, ct);
            }

            run.ProductsSeen = outcome.Products.Count;
            run.Status = outcome.PagesFetched == 0 ? RunStatus.Failed
                : outcome.PagesFailed > 0 || !outcome.Complete ? RunStatus.PartiallySucceeded
                : RunStatus.Succeeded;
            if (run.Status == RunStatus.Failed)
            {
                run.Error = "No pages could be fetched.";
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            run.Status = RunStatus.Failed;
            run.Error = "Cancelled (shutdown).";
            throw;
        }
        catch (StoreBlockedException ex)
        {
            run.Status = RunStatus.Failed;
            run.Error = Truncate(ex.Message);
            log.LogWarning("Collection stopped: {Reason}", ex.Message);
        }
        catch (Exception ex)
        {
            run.Status = RunStatus.Failed;
            run.Error = Truncate($"{ex.GetType().Name}: {ex.Message}");
            log.LogError(ex, "Collection failed");
        }
        finally
        {
            run.FinishedAt = time.GetUtcNow();
            db.ChangeTracker.Clear();
            db.CollectionRuns.Update(run);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        log.LogInformation("Collection finished: {Status}, offers={Offers}, pages={Pages}, failedPages={Failed}",
            run.Status, run.OffersActive, run.PagesFetched, run.PagesFailed);
        return run.Status;
    }

    /// <summary>True when the store has no finished, at least partially successful run within the interval.</summary>
    public async Task<bool> IsDueAsync(string storeSlug, TimeSpan interval, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var since = time.GetUtcNow() - interval;
        var recentOk = await db.CollectionRuns.AnyAsync(r =>
            r.StoreSlug == storeSlug && r.StartedAt >= since &&
            (r.Status == RunStatus.Succeeded || r.Status == RunStatus.PartiallySucceeded), ct);
        if (recentOk)
        {
            return false;
        }

        // Back off for an hour after a failed run so a blocked store is not hammered.
        var recentFailure = await db.CollectionRuns.AnyAsync(r =>
            r.StoreSlug == storeSlug && r.StartedAt >= time.GetUtcNow().AddHours(-1) && r.Status != RunStatus.Running, ct);
        return !recentFailure;
    }

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500];
}
