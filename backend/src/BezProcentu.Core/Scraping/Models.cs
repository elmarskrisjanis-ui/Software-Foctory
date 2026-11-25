namespace BezProcentu.Core.Scraping;

/// <summary>Whether the 0% offer is attached to a specific product or applies store-wide within a price range.</summary>
public enum OfferScope
{
    Product = 1,
    StoreWide = 2,
}

/// <summary>Interest-free financing terms as published by the store.</summary>
public sealed record LeasingTerms(
    OfferScope Scope,
    decimal InterestRatePercent,
    int MaxTermMonths,
    IReadOnlyList<int> TermsMonths,
    decimal? MinAmount,
    decimal? MaxAmount,
    string Provider,
    string Conditions,
    string SourceUrl)
{
    public bool IsInterestFree => InterestRatePercent == 0m && MaxTermMonths > 0;

    public bool AppliesToPrice(decimal price) =>
        (MinAmount is null || price >= MinAmount) && (MaxAmount is null || price <= MaxAmount);
}

/// <summary>A product row parsed from a store listing page, before financing is attached.</summary>
public sealed record ListedProduct(
    string StoreProductId,
    string Name,
    string Url,
    string? ImageUrl,
    decimal Price,
    decimal? OldPrice,
    string? Brand,
    LeasingTerms? ProductOffer = null,
    string? ImageFallbackUrl = null);

/// <summary>A product that has an active interest-free offer, ready to be stored.</summary>
public sealed record CollectedProduct(
    string StoreProductId,
    string Name,
    string Url,
    string? ImageUrl,
    decimal Price,
    decimal? OldPrice,
    string? Brand,
    string CategorySlug,
    LeasingTerms Offer,
    string? ImageFallbackUrl = null)
{
    /// <summary>Monthly payment at the longest 0% term, excluding any contract fee.</summary>
    public decimal MonthlyPayment => Math.Round(Price / Offer.MaxTermMonths, 2, MidpointRounding.AwayFromZero);
}
