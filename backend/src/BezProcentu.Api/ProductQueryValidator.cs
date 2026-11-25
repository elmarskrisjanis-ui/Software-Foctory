using System.Globalization;
using BezProcentu.Core.Catalog;
using BezProcentu.Core.Stores;
using BezProcentu.Data;

namespace BezProcentu.Api;

/// <summary>Raw query string values as received; everything is validated before it reaches the database.</summary>
public sealed record ProductQueryInput(
    string? Group,
    string? Category,
    string? Store,
    string? Q,
    string? MinPrice,
    string? MaxPrice,
    string? Sort,
    string? Page,
    string? PageSize);

public static class ProductQueryValidator
{
    public const int MaxSearchLength = 100;
    public const int MaxPageSize = 60;
    public const int DefaultPageSize = 24;
    public const int MaxPage = 500;

    private static readonly Dictionary<string, ProductSort> Sorts = new(StringComparer.Ordinal)
    {
        ["monthly-asc"] = ProductSort.MonthlyAsc,
        ["price-asc"] = ProductSort.PriceAsc,
        ["price-desc"] = ProductSort.PriceDesc,
        ["term-desc"] = ProductSort.TermDesc,
        ["newest"] = ProductSort.Newest,
    };

    public static IReadOnlyCollection<string> SortValues => Sorts.Keys;

    public static (ProductQuery? Query, Dictionary<string, string[]> Errors) Validate(ProductQueryInput input)
    {
        var errors = new Dictionary<string, string[]>();

        var group = Empty(input.Group);
        if (group is not null && !ProductCatalog.IsGroup(group))
        {
            errors["group"] = ["Unknown group."];
        }

        var category = Empty(input.Category);
        if (category is not null && !ProductCatalog.IsCategory(category))
        {
            errors["category"] = ["Unknown category."];
        }

        var store = Empty(input.Store);
        if (store is not null && StoreSources.Find(store) is not { Status: StoreStatus.Active })
        {
            errors["store"] = ["Unknown store."];
        }

        var search = Empty(input.Q)?.Trim();
        if (search is not null && (search.Length > MaxSearchLength || search.Any(char.IsControl)))
        {
            errors["q"] = [$"Search text must be at most {MaxSearchLength} characters without control characters."];
        }

        var minPrice = Price(input.MinPrice, "minPrice", errors);
        var maxPrice = Price(input.MaxPrice, "maxPrice", errors);
        if (minPrice is not null && maxPrice is not null && minPrice > maxPrice)
        {
            errors["maxPrice"] = ["maxPrice must be greater than or equal to minPrice."];
        }

        var sort = ProductSort.MonthlyAsc;
        if (Empty(input.Sort) is { } s && !Sorts.TryGetValue(s, out sort))
        {
            errors["sort"] = [$"Allowed values: {string.Join(", ", Sorts.Keys)}."];
        }

        var page = Int(input.Page, 1, 1, MaxPage, "page", errors);
        var pageSize = Int(input.PageSize, DefaultPageSize, 1, MaxPageSize, "pageSize", errors);

        return errors.Count > 0
            ? (null, errors)
            : (new ProductQuery(group, category, store, string.IsNullOrEmpty(search) ? null : search, minPrice, maxPrice, sort, page, pageSize), errors);
    }

    private static string? Empty(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;

    private static decimal? Price(string? raw, string name, Dictionary<string, string[]> errors)
    {
        if (Empty(raw) is null)
        {
            return null;
        }

        if (raw!.Length > 12 || !decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var v) || v < 0 || v > 1_000_000)
        {
            errors[name] = ["Must be a number between 0 and 1000000."];
            return null;
        }

        return v;
    }

    private static int Int(string? raw, int fallback, int min, int max, string name, Dictionary<string, string[]> errors)
    {
        if (Empty(raw) is null)
        {
            return fallback;
        }

        if (raw!.Length > 6 || !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var v) || v < min || v > max)
        {
            errors[name] = [$"Must be an integer between {min} and {max}."];
            return fallback;
        }

        return v;
    }
}
