using BezProcentu.Core.Scraping;

namespace BezProcentu.Data;

public sealed class ProductEntity
{
    public long Id { get; set; }
    public required string StoreSlug { get; set; }
    public required string StoreProductId { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageFallbackUrl { get; set; }
    public string? Brand { get; set; }
    public required string CategorySlug { get; set; }
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }

    // Interest-free offer as last verified at the store.
    public bool IsOfferActive { get; set; }
    public OfferScope OfferScope { get; set; }
    public decimal InterestRatePercent { get; set; }
    public int MaxTermMonths { get; set; }
    public int[] TermsMonths { get; set; } = [];
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
    public required string Provider { get; set; }
    public required string Conditions { get; set; }
    public required string OfferSourceUrl { get; set; }
    public DateTimeOffset OfferVerifiedAt { get; set; }
}

public enum RunStatus
{
    Running = 1,
    Succeeded = 2,
    PartiallySucceeded = 3,
    Failed = 4,
}

/// <summary>One collection run for one store. Used for freshness display and to notice failing collectors.</summary>
public sealed class CollectionRunEntity
{
    public long Id { get; set; }
    public required string StoreSlug { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public RunStatus Status { get; set; }
    public int PagesFetched { get; set; }
    public int PagesFailed { get; set; }
    public int ProductsSeen { get; set; }
    public int OffersActive { get; set; }
    public bool StorePolicyActive { get; set; }
    public string? Error { get; set; }
}
