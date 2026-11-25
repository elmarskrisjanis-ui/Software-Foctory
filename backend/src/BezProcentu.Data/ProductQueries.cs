using BezProcentu.Core.Catalog;
using BezProcentu.Core.Scraping;
using BezProcentu.Core.Stores;
using Microsoft.EntityFrameworkCore;

namespace BezProcentu.Data;

public enum ProductSort
{
    MonthlyAsc,
    PriceAsc,
    PriceDesc,
    TermDesc,
    Newest,
}

/// <summary>Validated query. Construct through <see cref="ProductQueryValidator"/>.</summary>
public sealed record ProductQuery(
    string? Group,
    string? Category,
    string? Store,
    string? Search,
    decimal? MinPrice,
    decimal? MaxPrice,
    ProductSort Sort,
    int Page,
    int PageSize);

public sealed record ProductDto(
    long Id,
    string Store,
    string StoreName,
    string Name,
    string Url,
    string? ImageUrl,
    string? ImageFallbackUrl,
    string? Brand,
    string Category,
    string CategoryName,
    string Group,
    decimal Price,
    decimal? OldPrice,
    decimal MonthlyPayment,
    int MaxTermMonths,
    int[] TermsMonths,
    decimal InterestRatePercent,
    string OfferScope,
    string Provider,
    string Conditions,
    string OfferSourceUrl,
    DateTimeOffset VerifiedAt);

public sealed record ProductPage(IReadOnlyList<ProductDto> Items, int Total, int Page, int PageSize);

public sealed record FacetCount(string Slug, string Name, string? Group, int Count);

public sealed record Facets(IReadOnlyList<FacetCount> Groups, IReadOnlyList<FacetCount> Categories, IReadOnlyList<FacetCount> Stores, int Total);

public sealed class ProductQueries(AppDbContext db, TimeProvider time, OfferFreshness freshness)
{
    /// <summary>Only offers verified recently are shown; anything older may have expired at the store.</summary>
    public IQueryable<ProductEntity> Visible()
    {
        var cutoff = time.GetUtcNow() - freshness.MaxAge;
        return db.Products.AsNoTracking().Where(p => p.IsOfferActive && p.OfferVerifiedAt >= cutoff && p.InterestRatePercent == 0m);
    }

    public async Task<ProductPage> SearchAsync(ProductQuery q, CancellationToken ct)
    {
        var query = ApplyFilters(Visible(), q, includeCategory: true, includeStore: true);
        var total = await query.CountAsync(ct);

        query = q.Sort switch
        {
            ProductSort.PriceAsc => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
            ProductSort.PriceDesc => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            ProductSort.TermDesc => query.OrderByDescending(p => p.MaxTermMonths).ThenBy(p => p.MonthlyPayment).ThenBy(p => p.Id),
            ProductSort.Newest => query.OrderByDescending(p => p.FirstSeenAt).ThenBy(p => p.Id),
            _ => query.OrderBy(p => p.MonthlyPayment).ThenBy(p => p.Id),
        };

        var rows = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        return new ProductPage(rows.Select(ToDto).ToList(), total, q.Page, q.PageSize);
    }

    public async Task<ProductDto?> GetAsync(long id, CancellationToken ct)
    {
        var row = await Visible().FirstOrDefaultAsync(p => p.Id == id, ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<Facets> FacetsAsync(ProductQuery q, CancellationToken ct)
    {
        // Category counts respect store/search/price filters; store counts respect category/search/price filters.
        var byCategory = await ApplyFilters(Visible(), q, includeCategory: false, includeStore: true)
            .GroupBy(p => p.CategorySlug)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var byStore = await ApplyFilters(Visible(), q, includeCategory: true, includeStore: false)
            .GroupBy(p => p.StoreSlug)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var categories = ProductCatalog.Categories
            .OrderBy(c => ProductCatalog.Groups.First(g => g.Slug == c.GroupSlug).SortOrder).ThenBy(c => c.SortOrder)
            .Select(c => new FacetCount(c.Slug, c.Name, c.GroupSlug, byCategory.GetValueOrDefault(c.Slug)))
            .ToList();

        var groups = ProductCatalog.Groups
            .OrderBy(g => g.SortOrder)
            .Select(g => new FacetCount(g.Slug, g.Name, null, categories.Where(c => c.Group == g.Slug).Sum(c => c.Count)))
            .ToList();

        var stores = StoreSources.All
            .Where(s => s.Status == StoreStatus.Active)
            .Select(s => new FacetCount(s.Slug, s.Name, null, byStore.GetValueOrDefault(s.Slug)))
            .ToList();

        return new Facets(groups, categories, stores, groups.Sum(g => g.Count));
    }

    private static IQueryable<ProductEntity> ApplyFilters(IQueryable<ProductEntity> query, ProductQuery q, bool includeCategory, bool includeStore)
    {
        if (includeCategory && q.Category is not null)
        {
            query = query.Where(p => p.CategorySlug == q.Category);
        }
        else if (includeCategory && q.Group is not null)
        {
            var slugs = ProductCatalog.CategoriesInGroup(q.Group).ToList();
            query = query.Where(p => slugs.Contains(p.CategorySlug));
        }

        if (includeStore && q.Store is not null)
        {
            query = query.Where(p => p.StoreSlug == q.Store);
        }

        if (q.MinPrice is not null)
        {
            query = query.Where(p => p.Price >= q.MinPrice);
        }

        if (q.MaxPrice is not null)
        {
            query = query.Where(p => p.Price <= q.MaxPrice);
        }

        if (!string.IsNullOrEmpty(q.Search))
        {
            // Parameterised ILIKE; wildcard characters in user input are escaped.
            var pattern = "%" + EscapeLike(q.Search) + "%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, "\\") || (p.Brand != null && EF.Functions.ILike(p.Brand, pattern, "\\")));
        }

        return query;
    }

    internal static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static ProductDto ToDto(ProductEntity p)
    {
        var category = ProductCatalog.IsCategory(p.CategorySlug) ? ProductCatalog.GetCategory(p.CategorySlug) : null;
        return new ProductDto(
            p.Id,
            p.StoreSlug,
            StoreSources.Find(p.StoreSlug)?.Name ?? p.StoreSlug,
            p.Name,
            p.Url,
            p.ImageUrl,
            p.ImageFallbackUrl,
            p.Brand,
            p.CategorySlug,
            category?.Name ?? p.CategorySlug,
            category?.GroupSlug ?? string.Empty,
            p.Price,
            p.OldPrice,
            p.MonthlyPayment,
            p.MaxTermMonths,
            p.TermsMonths,
            p.InterestRatePercent,
            p.OfferScope == OfferScope.Product ? "product" : "store",
            p.Provider,
            p.Conditions,
            p.OfferSourceUrl,
            p.OfferVerifiedAt);
    }
}

public sealed record OfferFreshness(TimeSpan MaxAge);
