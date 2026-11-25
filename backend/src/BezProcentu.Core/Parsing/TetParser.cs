using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using BezProcentu.Core.Scraping;

namespace BezProcentu.Core.Parsing;

/// <summary>
/// Tet.lv e-veikals: the financing page advertises interest-free instalments ("Bezprocentu nomaksa uz 3, 6 un 12
/// mēnešu termiņu") for purchases in a stated range. Individual products may also carry a "0% akcija"
/// in the listing's data-split-payment-prices attribute.
/// </summary>
public static partial class TetParser
{
    public static IReadOnlyList<ListedProduct> ParseListing(string html, Uri pageUri, IReadOnlyCollection<string> siteHosts, IReadOnlyCollection<string> imageHosts)
    {
        var doc = new HtmlParser().ParseDocument(html);
        var result = new List<ListedProduct>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var card in doc.QuerySelectorAll("div.i-product-item[data-id][data-name]"))
        {
            // Landing pages also show "discount"/"new products" rows from other categories; skip those rows.
            if (card.Closest("[aria-labelledby$='-products-title']") is not null)
            {
                continue;
            }

            var id = card.GetAttribute("data-id")!.Trim();
            if (!IdRegex().IsMatch(id) || !seen.Add(id))
            {
                continue;
            }

            var url = ParseHelpers.SafeUrl(card.QuerySelector("a.i-product-item__link")?.GetAttribute("href"), pageUri, siteHosts);
            var name = ParseHelpers.CleanText(card.GetAttribute("data-name"));
            var pricesEl = card.QuerySelector("[data-pricing]");
            var (price, msrp) = ParsePricing(pricesEl?.GetAttribute("data-pricing"));
            price ??= ParseHelpers.ParsePrice(card.GetAttribute("data-price"));
            if (url is null || name.Length == 0 || price is null)
            {
                continue;
            }

            var image = ParseHelpers.SafeUrl(card.QuerySelector("img.i-product-item__image-img")?.GetAttribute("src"), pageUri, imageHosts);
            var brand = ParseHelpers.CleanText(card.GetAttribute("data-brand"), 80);
            var productOffer = ParseSplitPaymentOffer(pricesEl?.GetAttribute("data-split-payment-prices"), url);

            result.Add(new ListedProduct(id, name, url, image, price.Value, msrp > price ? msrp : null,
                brand.Length > 0 ? brand : null, productOffer));
        }

        return result;
    }

    public static LeasingTerms? ParseStorePolicy(string html, Uri policyUri)
    {
        var doc = new HtmlParser().ParseDocument(html);
        var text = ParseHelpers.CleanText(doc.Body?.TextContent, 20000);

        var offer = InterestFreeTermsRegex().Match(text);
        if (!offer.Success)
        {
            return null;
        }

        var terms = TermNumberRegex().Matches(offer.Groups[1].Value)
            .Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture))
            .Where(t => t is > 0 and <= 120)
            .Distinct()
            .Order()
            .ToList();
        if (terms.Count == 0)
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

        var conditions = $"Bezprocentu nomaksa uz {string.Join(", ", terms)} mēn. (aizņēmuma likme 0%)."
            + (min is not null && max is not null ? $" Pirkuma summa {min:0.##}–{max:0.##} €." : string.Empty)
            + " Var tikt piemērota maksa par aizdevuma izpildi, tāpēc GPL var būt lielāka par 0%."
            + " Pirkumiem no 1400 € obligāta pirmā iemaksa no 10%.";

        return new LeasingTerms(OfferScope.StoreWide, 0m, terms.Max(), terms, min, max, "InCredit Group", conditions, policyUri.AbsoluteUri);
    }

    private static (decimal? Price, decimal? Msrp) ParsePricing(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 4000)
        {
            return (null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return (ReadPositive(doc.RootElement, "price"), ReadPositive(doc.RootElement, "msrp"));
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Interprets a non-empty data-split-payment-prices array. The format is undocumented, so only entries that
    /// clearly state a 0 rate together with a term are accepted; anything else is ignored.
    /// </summary>
    internal static LeasingTerms? ParseSplitPaymentOffer(string? json, string productUrl)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 8000)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var terms = new List<int>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                int? term = null;
                decimal? rate = null;
                foreach (var prop in item.EnumerateObject())
                {
                    var key = prop.Name.ToLowerInvariant();
                    if (term is null && (key.Contains("term") || key.Contains("month")) && TryNumber(prop.Value, out var t))
                    {
                        term = (int)t;
                    }
                    else if (rate is null && (key.Contains("rate") || key.Contains("interest") || key.Contains("percent")) && TryNumber(prop.Value, out var r))
                    {
                        rate = r;
                    }
                }

                if (term is > 0 and <= 120 && rate == 0m)
                {
                    terms.Add(term.Value);
                }
            }

            if (terms.Count == 0)
            {
                return null;
            }

            terms = terms.Distinct().Order().ToList();
            return new LeasingTerms(OfferScope.Product, 0m, terms.Max(), terms, null, null, "InCredit Group",
                $"0% nomaksas akcija šai precei: {string.Join(", ", terms)} mēn.", productUrl);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static decimal? ReadPositive(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var el) && TryNumber(el, out var v) && v > 0
            ? Math.Round(v, 2)
            : null;

    private static bool TryNumber(JsonElement el, out decimal value)
    {
        value = 0;
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(el.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    [GeneratedRegex(@"^[0-9]{1,12}$")]
    private static partial Regex IdRegex();

    [GeneratedRegex(@"Bezprocentu nomaksa uz\s*([0-9, unvai]+?)\s*mēnešu termiņu", RegexOptions.IgnoreCase)]
    private static partial Regex InterestFreeTermsRegex();

    [GeneratedRegex(@"[0-9]+")]
    private static partial Regex TermNumberRegex();

    [GeneratedRegex(@"pieejama pirkumiem\s*([0-9][0-9 .,]*)\s*€\s*[-–]\s*([0-9][0-9 .,]*[0-9])\s*€", RegexOptions.IgnoreCase)]
    private static partial Regex AmountRangeRegex();
}
