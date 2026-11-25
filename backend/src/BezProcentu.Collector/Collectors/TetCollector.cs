using BezProcentu.Collector.Http;
using BezProcentu.Core.Parsing;
using BezProcentu.Core.Scraping;
using BezProcentu.Core.Stores;
using Microsoft.Extensions.Options;

namespace BezProcentu.Collector.Collectors;

/// <summary>Product-specific "0% akcija" wins; otherwise the store-wide interest-free instalment offer applies.</summary>
public sealed class TetCollector(PoliteFetcher fetcher, IOptions<CollectorOptions> options, ILogger<TetCollector> log)
    : StoreCollectorBase(fetcher, options, log)
{
    public override string StoreSlug => StoreSources.Tet;

    protected override Uri PageUri(ListingSource source, int page) => page == 1 ? new(source.Url) : new($"{source.Url}?page={page}");

    public override async Task<CollectionOutcome> CollectAsync(CancellationToken ct)
    {
        var policy = await FetchStorePolicyAsync(TetParser.ParseStorePolicy, ct);
        if (policy is null)
        {
            Log.LogInformation("{Store}: store-wide interest-free offer is not advertised any more", StoreSlug);
        }

        var products = new List<CollectedProduct>();
        foreach (var source in Store.Listings)
        {
            foreach (var (item, category) in await CrawlListingAsync(source, TetParser.ParseListing, ct))
            {
                var offer = item.ProductOffer is { IsInterestFree: true } ? item.ProductOffer
                    : policy is not null && policy.AppliesToPrice(item.Price) ? policy
                    : null;
                if (offer is not null)
                {
                    products.Add(ToCollected(item, category, offer));
                }
            }
        }

        return Outcome(products, policyActive: policy is not null);
    }
}
