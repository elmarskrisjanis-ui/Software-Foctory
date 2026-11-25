using System.Globalization;
using System.Text.RegularExpressions;

namespace BezProcentu.Core.Parsing;

public static partial class ParseHelpers
{
    /// <summary>
    /// Parses store price strings such as "538,02€", "1 299,00 €", "599.99" or "1.299,00".
    /// Returns null for anything that is not a plausible positive price.
    /// </summary>
    public static decimal? ParsePrice(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cleaned = NonPriceChars().Replace(text, string.Empty);
        if (cleaned.Length == 0 || cleaned.Length > 20)
        {
            return null;
        }

        var lastComma = cleaned.LastIndexOf(',');
        var lastDot = cleaned.LastIndexOf('.');
        if (lastComma >= 0 && lastDot >= 0)
        {
            // The later separator is the decimal separator; the other one groups thousands.
            cleaned = lastComma > lastDot
                ? cleaned.Replace(".", string.Empty).Replace(',', '.')
                : cleaned.Replace(",", string.Empty);
        }
        else if (lastComma >= 0)
        {
            cleaned = cleaned.Replace(',', '.');
        }

        if (!decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        return value is > 0m and < 1_000_000m ? Math.Round(value, 2) : null;
    }

    /// <summary>Collapses whitespace and trims; limits the length to protect storage and UI.</summary>
    public static string CleanText(string? text, int maxLength = 300)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var collapsed = Whitespace().Replace(text, " ").Trim();
        return collapsed.Length <= maxLength ? collapsed : collapsed[..maxLength].TrimEnd() + "…";
    }

    /// <summary>
    /// Resolves a (possibly relative) URL against the page URL and only accepts https URLs on allowed hosts.
    /// This keeps javascript:, data: and foreign links out of the stored data.
    /// </summary>
    public static string? SafeUrl(string? candidate, Uri pageUri, IReadOnlyCollection<string> allowedHosts)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (!Uri.TryCreate(pageUri, candidate.Trim(), out var uri))
        {
            return null;
        }

        if (uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return null;
        }

        var host = uri.IdnHost.ToLowerInvariant();
        return allowedHosts.Contains(host) && uri.AbsoluteUri.Length <= 1000 ? uri.AbsoluteUri : null;
    }

    [GeneratedRegex(@"[^0-9,.]")]
    private static partial Regex NonPriceChars();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
