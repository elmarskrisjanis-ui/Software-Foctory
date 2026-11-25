using BezProcentu.Core.Scraping;
using Microsoft.EntityFrameworkCore;

namespace BezProcentu.Data;

/// <summary>Stores the result of a collection run for one store.</summary>
public sealed class OfferWriter(AppDbContext db, TimeProvider time)
{
    /// <summary>
    /// Upserts collected products. When <paramref name="complete"/> is true (all pages fetched), every other
    /// product of the store is marked as no longer having an active offer, so expired offers disappear immediately.
    /// After a partial run, unseen products keep their old verification time and drop out once it is too old.
    /// </summary>
    public async Task<int> SaveAsync(string storeSlug, IReadOnlyCollection<CollectedProduct> products, bool complete, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var existing = await db.Products.Where(p => p.StoreSlug == storeSlug).ToDictionaryAsync(p => p.StoreProductId, ct);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in products)
        {
            if (!item.Offer.IsInterestFree || !seen.Add(item.StoreProductId))
            {
                continue;
            }

            if (!existing.TryGetValue(item.StoreProductId, out var row))
            {
                row = new ProductEntity
                {
                    StoreSlug = storeSlug,
                    StoreProductId = item.StoreProductId,
                    Name = item.Name,
                    Url = item.Url,
                    CategorySlug = item.CategorySlug,
                    Provider = item.Offer.Provider,
                    Conditions = item.Offer.Conditions,
                    OfferSourceUrl = item.Offer.SourceUrl,
                    FirstSeenAt = now,
                };
                db.Products.Add(row);
                existing[item.StoreProductId] = row;
            }

            row.Name = item.Name;
            row.Url = item.Url;
            row.ImageUrl = item.ImageUrl;
            row.ImageFallbackUrl = item.ImageFallbackUrl;
            row.Brand = item.Brand;
            row.CategorySlug = item.CategorySlug;
            row.Price = item.Price;
            row.OldPrice = item.OldPrice;
            row.LastSeenAt = now;
            row.IsOfferActive = true;
            row.OfferScope = item.Offer.Scope;
            row.InterestRatePercent = item.Offer.InterestRatePercent;
            row.MaxTermMonths = item.Offer.MaxTermMonths;
            row.TermsMonths = item.Offer.TermsMonths.ToArray();
            row.MinAmount = item.Offer.MinAmount;
            row.MaxAmount = item.Offer.MaxAmount;
            row.MonthlyPayment = item.MonthlyPayment;
            row.Provider = item.Offer.Provider;
            row.Conditions = item.Offer.Conditions;
            row.OfferSourceUrl = item.Offer.SourceUrl;
            row.OfferVerifiedAt = now;
        }

        if (complete)
        {
            foreach (var row in existing.Values.Where(r => r.IsOfferActive && !seen.Contains(r.StoreProductId)))
            {
                row.IsOfferActive = false;
            }
        }

        await db.SaveChangesAsync(ct);
        return seen.Count;
    }

    /// <summary>Used when a store withdraws its interest-free programme: hides all its offers at once.</summary>
    public Task<int> DeactivateStoreAsync(string storeSlug, CancellationToken ct) =>
        db.Products.Where(p => p.StoreSlug == storeSlug && p.IsOfferActive)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsOfferActive, false), ct);
}
