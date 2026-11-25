using BezProcentu.Collector.Http;
using BezProcentu.Core.Parsing;
using BezProcentu.Core.Scraping;
using BezProcentu.Core.Stores;
using Microsoft.Extensions.Options;

namespace BezProcentu.Collector.Collectors;

/// <summary>Store-wide 0% offer: every listed product whose price is inside the published range qualifies.</summary>
public sealed class M79Collector(PoliteFetcher fetcher, IOptions<CollectorOptions> options, ILogger<M79Collector> log)
    : StoreCollectorBase(fetcher, options, log)
{
    public override string StoreSlug => StoreSources.M79;

    protected override Uri PageUri(ListingSource source, int page) => new($"{source.Url}?limit=96&page={page}");

    public override async Task<CollectionOutcome> CollectAsync(CancellationToken ct)
    {
        var policy = await FetchStorePolicyAsync(M79Parser.ParseStorePolicy, ct);
        if (policy is null)
        {
            Log.LogInformation("{Store}: interest-free offer is not advertised any more", StoreSlug);
            return Outcome([], policyActive: false);
        }

        var products = new List<CollectedProduct>();
        foreach (var source in Store.Listings)
        {
            foreach (var (item, category) in await CrawlListingAsync(source, M79Parser.ParseListing, ct))
            {
                if (M79Parser.BelongsToListing(item.Url, source.Url) && policy.AppliesToPrice(item.Price))
                {
                    products.Add(ToCollected(item, category, policy));
                }
            }
        }

        return Outcome(products, policyActive: true);
    }
}
