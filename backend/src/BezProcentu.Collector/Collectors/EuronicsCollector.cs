using BezProcentu.Collector.Http;
using BezProcentu.Core.Parsing;
using BezProcentu.Core.Scraping;
using BezProcentu.Core.Stores;
using Microsoft.Extensions.Options;

namespace BezProcentu.Collector.Collectors;

/// <summary>
/// Euronics shows financing terms only on product pages, so each listed product page is fetched (rate-limited and
/// capped by MaxProductPagesPerRun). Only products whose calculator shows a 0% loan rate are kept.
/// </summary>
public sealed class EuronicsCollector(PoliteFetcher fetcher, IOptions<CollectorOptions> options, ILogger<EuronicsCollector> log)
    : StoreCollectorBase(fetcher, options, log)
{
    public override string StoreSlug => StoreSources.Euronics;

    protected override Uri PageUri(ListingSource source, int page) => page == 1 ? new(source.Url) : new($"{source.Url}?page={page}");

    public override async Task<CollectionOutcome> CollectAsync(CancellationToken ct)
    {
        var listed = new List<(ListedProduct Product, string Category)>();
        foreach (var source in Store.Listings)
        {
            listed.AddRange(await CrawlListingAsync(source, EuronicsParser.ParseListing, ct));
        }

        var products = new List<CollectedProduct>();
        var checkedPages = 0;
        var truncated = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (item, category) in listed)
        {
            if (!seen.Add(item.StoreProductId))
            {
                continue;
            }

            if (checkedPages >= Options.MaxProductPagesPerRun)
            {
                truncated = true;
                break;
            }

            checkedPages++;
            var uri = new Uri(item.Url);
            try
            {
                var html = await Fetcher.GetStringAsync(uri, ct);
                PagesFetched++;
                var terms = html is null ? null : EuronicsParser.ParseProductLeasing(html, uri);
                if (terms is { IsInterestFree: true })
                {
                    products.Add(ToCollected(item, category, terms));
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or DisallowedByRobotsException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                PagesFailed++;
                Log.LogWarning("{Store}: product page failed {Path}: {Reason}", StoreSlug, uri.AbsolutePath, ex.Message);
            }
        }

        Log.LogInformation("{Store}: checked {Checked} product pages, {Count} with 0% rate", StoreSlug, checkedPages, products.Count);
        return Outcome(products, policyActive: true, truncated);
    }
}
