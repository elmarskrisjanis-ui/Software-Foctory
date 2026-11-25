using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using BezProcentu.Core.Scraping;

namespace BezProcentu.Core.Parsing;

/// <summary>
/// M79.lv: interest-free credit is a store-wide offer described on /pages/bezprocentu-kredits
/// ("Bez % kredīts 3 mēnešiem ar aizdevuma likmi 0%"), valid for purchases in a stated amount range.
/// </summary>
public static partial class M79Parser
{
    public static IReadOnlyList<ListedProduct> ParseListing(string html, Uri pageUri, IReadOnlyCollection<string> siteHosts, IReadOnlyCollection<string> imageHosts)
    {
        var doc = new HtmlParser().ParseDocument(html);
        var result = new List<ListedProduct>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var card in doc.QuerySelectorAll("div.product_card"))
        {
            var link = card.QuerySelector("a[href*='/p/products/']");
            var url = ParseHelpers.SafeUrl(link?.GetAttribute("href"), pageUri, siteHosts);
            if (url is null)
            {
                continue;
            }

            var idMatch = ProductIdRegex().Match(url);
            if (!idMatch.Success || !seen.Add(idMatch.Groups[1].Value))
            {
                continue;
            }

            var name = ParseHelpers.CleanText(card.QuerySelector(".card_title")?.TextContent);
            if (name.EndsWith("...", StringComparison.Ordinal))
            {
                name = name[..^3].TrimEnd() + "…";
            }

            var price = ParseHelpers.ParsePrice(card.QuerySelector(".card_price .now")?.TextContent);
            if (name.Length == 0 || price is null)
            {
                continue;
            }

            var oldPrice = ParseHelpers.ParsePrice(card.QuerySelector(".card_price .before")?.TextContent);
            var figure = card.QuerySelector("figure.card_img");
            var image = ParseHelpers.SafeUrl(figure?.GetAttribute("data-src-bg"), pageUri, imageHosts);
            var fallback = ParseHelpers.SafeUrl(figure?.GetAttribute("data-src-bg-fallback"), pageUri, imageHosts);
            var brand = BrandRegex().Match(url) is { Success: true } b ? Uri.UnescapeDataString(b.Groups[1].Value) : null;

            result.Add(new ListedProduct(idMatch.Groups[1].Value, name, url, image ?? fallback, price.Value,
                oldPrice > price ? oldPrice : null, brand is null ? null : ParseHelpers.CleanText(brand, 80),
                ImageFallbackUrl: fallback != image ? fallback : null));
        }

        return result;
    }

    /// <summary>
    /// True when the product URL (/p/products/{id}/{Category}/...) belongs to the listing's category
    /// (/c/category/{Category}). Some M79 category pages pad results with unrelated products.
    /// </summary>
    public static bool BelongsToListing(string productUrl, string listingUrl)
    {
        var product = ProductCategoryRegex().Match(productUrl);
        var listing = ListingCategoryRegex().Match(listingUrl);
        return product.Success && listing.Success &&
            string.Equals(Uri.UnescapeDataString(product.Groups[1].Value.Replace('+', ' ')),
                Uri.UnescapeDataString(listing.Groups[1].Value.Replace('+', ' ')), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses the store-wide interest-free offer. Returns null if the page no longer advertises a 0% rate,
    /// which deactivates all M79 offers on the next run.
    /// </summary>
    public static LeasingTerms? ParseStorePolicy(string html, Uri policyUri)
    {
        var doc = new HtmlParser().ParseDocument(html);
        var text = ParseHelpers.CleanText(doc.Body?.TextContent, 20000);

        var offer = ZeroRateRegex().Match(text);
        if (!offer.Success)
        {
            return null;
        }

        var term = int.Parse(offer.Groups[1].Value, CultureInfo.InvariantCulture);
        if (term is < 1 or > 120)
        {
            return null;
        }

        decimal? min = null, max = null;
        var range = AmountRangeRegex().Match(text);
        if (range.Success)
        {
            min = ParseHelpers.ParsePrice(range.Groups[1].Value);
            max = ParseHelpers.ParsePrice(range.Groups[2].Value);
        }

        var conditions = $"Aizdevuma likme 0%, termiņš {term} mēn."
            + (min is not null && max is not null ? $" Pirkuma summa {min:0.##}–{max:0.##} €." : string.Empty)
            + " Pirkumiem virs 1400 € var prasīt 10% pirmo iemaksu. Kredītu piešķir pēc kredītspējas izvērtēšanas.";

        return new LeasingTerms(OfferScope.StoreWide, 0m, term, [term], min, max, "Aizdevums.lv", conditions, policyUri.AbsoluteUri);
    }

    [GeneratedRegex(@"/p/products/([0-9]{1,12})(?:/|$)")]
    private static partial Regex ProductIdRegex();

    [GeneratedRegex(@"/p/products/[0-9]+/([^/]+)/")]
    private static partial Regex ProductCategoryRegex();

    [GeneratedRegex(@"/c/category/([^/?#]+)")]
    private static partial Regex ListingCategoryRegex();

    [GeneratedRegex(@"/p/products/[0-9]+/[^/]+/([^/]+)/")]
    private static partial Regex BrandRegex();

    [GeneratedRegex(@"([0-9]{1,3})\s*mēnešiem\*?\s*ar aizdevuma likmi 0\s*%", RegexOptions.IgnoreCase)]
    private static partial Regex ZeroRateRegex();

    [GeneratedRegex(@"Aizdevuma summa sākot no EUR\s*([0-9][0-9 .,]*)\s*līdz EUR\s*([0-9][0-9 .,]*[0-9])", RegexOptions.IgnoreCase)]
    private static partial Regex AmountRangeRegex();
}
