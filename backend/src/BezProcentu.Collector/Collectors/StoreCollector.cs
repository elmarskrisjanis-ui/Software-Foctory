using BezProcentu.Collector.Http;
using BezProcentu.Core.Scraping;
using BezProcentu.Core.Stores;
using Microsoft.Extensions.Options;

namespace BezProcentu.Collector.Collectors;

public sealed record CollectionOutcome(
    IReadOnlyList<CollectedProduct> Products,
    bool StorePolicyActive,
    int PagesFetched,
    int PagesFailed,
    bool Complete);

public interface IStoreCollector
{
    string StoreSlug { get; }

    Task<CollectionOutcome> CollectAsync(CancellationToken ct);
}

public delegate IReadOnlyList<ListedProduct> ListingParser(string html, Uri pageUri, IReadOnlyCollection<string> siteHosts, IReadOnlyCollection<string> imageHosts);

/// <summary>Shared paging logic for store listing pages.</summary>
public abstract class StoreCollectorBase(PoliteFetcher fetcher, IOptions<CollectorOptions> options, ILogger log) : IStoreCollector
{
    protected PoliteFetcher Fetcher { get; } = fetcher;
    protected CollectorOptions Options { get; } = options.Value;
    protected ILogger Log { get; } = log;
    protected int PagesFetched { get; set; }
    protected int PagesFailed { get; set; }

    public abstract string StoreSlug { get; }

    protected StoreDefinition Store => StoreSources.Find(StoreSlug)!;

    public abstract Task<CollectionOutcome> CollectAsync(CancellationToken ct);

    protected abstract Uri PageUri(ListingSource source, int page);

    /// <summary>Returns every product of one listing (all pages up to the configured limit), tagged with its category.</summary>
    protected async Task<List<(ListedProduct Product, string Category)>> CrawlListingAsync(ListingSource source, ListingParser parser, CancellationToken ct)
    {
        var result = new List<(ListedProduct, string)>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var page = 1; page <= Options.MaxPagesPerListing; page++)
        {
            var uri = PageUri(source, page);
            string? html;
            try
            {
                html = await Fetcher.GetStringAsync(uri, ct);
                PagesFetched++;
            }
            catch (Exception ex) when (ex is HttpRequestException or DisallowedByRobotsException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                PagesFailed++;
                Log.LogWarning("{Store}: listing page failed {Path}: {Reason}", StoreSlug, uri.AbsolutePath, ex.Message);
                break;
            }

            if (html is null)
            {
                if (page == 1)
                {
                    PagesFailed++;
                    Log.LogWarning("{Store}: listing not found {Path}", StoreSlug, uri.AbsolutePath);
                }

                break;
            }

            var items = parser(html, uri, Store.SiteHosts, Store.ImageHosts);
            var added = 0;
            foreach (var item in items.Where(i => ids.Add(i.StoreProductId)))
            {
                result.Add((item, source.CategorySlug));
                added++;
            }

            if (added == 0)
            {
                break;
            }
        }

        return result;
    }

    protected async Task<LeasingTerms?> FetchStorePolicyAsync(Func<string, Uri, LeasingTerms?> parse, CancellationToken ct)
    {
        var uri = new Uri(Store.PolicyUrl!);
        var html = await Fetcher.GetStringAsync(uri, ct);
        PagesFetched++;
        return html is null ? null : parse(html, uri);
    }

    protected CollectionOutcome Outcome(IReadOnlyList<CollectedProduct> products, bool policyActive, bool truncated = false) =>
        new(products, policyActive, PagesFetched, PagesFailed, Complete: PagesFailed == 0 && !truncated);

    protected static CollectedProduct ToCollected(ListedProduct p, string category, LeasingTerms offer) =>
        new(p.StoreProductId, p.Name, p.Url, p.ImageUrl, p.Price, p.OldPrice, p.Brand, category, offer, p.ImageFallbackUrl);
}
