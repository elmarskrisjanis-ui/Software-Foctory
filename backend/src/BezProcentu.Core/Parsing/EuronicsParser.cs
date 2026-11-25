using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using BezProcentu.Core.Scraping;

namespace BezProcentu.Core.Parsing;

/// <summary>
/// Euronics.lv: listing pages give product data, and each product page contains a leasing calculator
/// with the store's financing terms for that product ("Aizņēmuma likme X %", "Līzinga periods N mēneši").
/// </summary>
public static partial class EuronicsParser
{
    public static IReadOnlyList<ListedProduct> ParseListing(string html, Uri pageUri, IReadOnlyCollection<string> siteHosts, IReadOnlyCollection<string> imageHosts)
    {
        var doc = new HtmlParser().ParseDocument(html);
        var result = new List<ListedProduct>();

        foreach (var card in doc.QuerySelectorAll("article.product-card[data-product-id]"))
        {
            var id = card.GetAttribute("data-product-id")?.Trim();
            var link = card.QuerySelector("a.product_name") ?? card.QuerySelector("a[href]");
            var url = ParseHelpers.SafeUrl(link?.GetAttribute("href"), pageUri, siteHosts);
            var name = ParseHelpers.CleanText(card.QuerySelector(".product-card__title")?.TextContent);
            if (name.Length == 0)
            {
                name = ParseHelpers.CleanText(card.GetAttribute("data-product-name"));
            }

            var price = ParseHelpers.ParsePrice(card.GetAttribute("data-product-price"));
            if (string.IsNullOrEmpty(id) || url is null || name.Length == 0 || price is null)
            {
                continue;
            }

            var image = ParseHelpers.SafeUrl(card.QuerySelector("img.product-card__image")?.GetAttribute("src"), pageUri, imageHosts);
            var brand = card.GetAttribute("data-product-brand");
            if (string.Equals(brand, "Vaikimisi", StringComparison.OrdinalIgnoreCase))
            {
                brand = null; // placeholder value used by the store
            }

            result.Add(new ListedProduct(id, name, url, image, price.Value, null, ParseHelpers.CleanText(brand, 80) is { Length: > 0 } b ? b : null));
        }

        return result;
    }

    /// <summary>Reads the leasing calculator of a product page. Returns null when no calculator is present.</summary>
    public static LeasingTerms? ParseProductLeasing(string html, Uri productUri)
    {
        var doc = new HtmlParser().ParseDocument(html);
        var info = doc.QuerySelector("#leaseInformation .tooltip__content") ?? doc.QuerySelector("#lease-calculator-container");
        if (info is null)
        {
            return null;
        }

        var text = ParseHelpers.CleanText(info.TextContent, 2000);
        var rateMatch = RateRegex().Match(text);
        var termMatch = TermRegex().Match(text);
        if (!rateMatch.Success || !termMatch.Success)
        {
            return null;
        }

        var rate = decimal.Parse(rateMatch.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        var term = int.Parse(termMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        if (term is < 1 or > 120)
        {
            return null;
        }

        var provider = doc.QuerySelector("#lease-calculator-container img.calculator__provider-logo")?.GetAttribute("alt");
        var fee = FeeRegex().Match(text);
        var conditions = $"Aizņēmuma likme {rate.ToString(CultureInfo.InvariantCulture)}%, termiņš līdz {term} mēn."
            + (fee.Success ? $" Komisijas maksa {fee.Groups[1].Value} €." : string.Empty);

        return new LeasingTerms(
            OfferScope.Product,
            rate,
            term,
            [term],
            null,
            null,
            ParseHelpers.CleanText(provider, 60) is { Length: > 0 } p ? p : "Euronics līzings",
            conditions,
            productUri.AbsoluteUri);
    }

    [GeneratedRegex(@"Aizņēmuma likme\s*([0-9]+(?:[.,][0-9]+)?)\s*%", RegexOptions.IgnoreCase)]
    private static partial Regex RateRegex();

    [GeneratedRegex(@"Līzinga periods\s*([0-9]{1,3})\s*mēneš", RegexOptions.IgnoreCase)]
    private static partial Regex TermRegex();

    [GeneratedRegex(@"Komisijas maksa\s*([0-9]+(?:[.,][0-9]+)?)\s*€", RegexOptions.IgnoreCase)]
    private static partial Regex FeeRegex();
}
