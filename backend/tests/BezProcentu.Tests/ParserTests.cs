using BezProcentu.Core.Parsing;
using BezProcentu.Core.Scraping;

namespace BezProcentu.Tests;

public class ParseHelpersTests
{
    [Theory]
    [InlineData("538,02€", 538.02)]
    [InlineData("1 249,00 €", 1249.00)]
    [InlineData("599.99", 599.99)]
    [InlineData("1.299,50", 1299.50)]
    [InlineData("1,299.50", 1299.50)]
    public void ParsePrice_accepts_store_formats(string input, double expected) =>
        Assert.Equal((decimal)expected, ParseHelpers.ParsePrice(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("99999999999999999999999")]
    public void ParsePrice_rejects_invalid(string? input) => Assert.Null(ParseHelpers.ParsePrice(input));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<b>x</b>")]
    [InlineData("http://www.m79.lv/p/1")]
    [InlineData("https://evil.example.com/p/1")]
    [InlineData("https://user:pw@www.m79.lv/p/1")]
    [InlineData("https://www.m79.lv:8443/p/1")]
    public void SafeUrl_rejects_unsafe_or_foreign_urls(string candidate) =>
        Assert.Null(ParseHelpers.SafeUrl(candidate, new Uri("https://www.m79.lv/c/x"), ["www.m79.lv"]));

    [Fact]
    public void SafeUrl_resolves_relative_urls() =>
        Assert.Equal("https://www.m79.lv/p/products/1/a",
            ParseHelpers.SafeUrl("/p/products/1/a", new Uri("https://www.m79.lv/c/x"), ["www.m79.lv"]));

    [Fact]
    public void CleanText_collapses_whitespace_and_limits_length()
    {
        Assert.Equal("a b c", ParseHelpers.CleanText("  a \n\t b   c "));
        Assert.Equal(11, ParseHelpers.CleanText(new string('x', 50), 10).Length);
    }
}

public class EuronicsParserTests
{
    private static readonly Uri Page = new("https://www.euronics.lv/tv/televizori");

    [Fact]
    public void Listing_parses_valid_cards_and_drops_unsafe_ones()
    {
        var items = EuronicsParser.ParseListing(Fixture.Read("euronics-listing.html"), Page, ["www.euronics.lv"], ["www.euronics.lv"]);

        Assert.Equal(2, items.Count);
        var tv = items[0];
        Assert.Equal("1001", tv.StoreProductId);
        Assert.Equal("Test TV 55'' OLED - Televizors", tv.Name);
        Assert.Equal(799.99m, tv.Price);
        Assert.Null(tv.Brand); // "Vaikimisi" placeholder is dropped
        Assert.StartsWith("https://www.euronics.lv/UserFiles/", tv.ImageUrl);

        var second = items[1];
        Assert.Equal("https://www.euronics.lv/tv/televizori/led/tv32/test-tv-32", second.Url);
        Assert.Null(second.ImageUrl); // foreign image host rejected
    }

    [Fact]
    public void Product_page_with_zero_rate_is_interest_free()
    {
        var terms = EuronicsParser.ParseProductLeasing(Fixture.Read("euronics-product-zero.html"), new Uri("https://www.euronics.lv/p/1"));

        Assert.NotNull(terms);
        Assert.True(terms.IsInterestFree);
        Assert.Equal(24, terms.MaxTermMonths);
        Assert.Equal(OfferScope.Product, terms.Scope);
        Assert.Equal("InBank", terms.Provider);
    }

    [Fact]
    public void Product_page_with_regular_rate_is_not_interest_free()
    {
        var terms = EuronicsParser.ParseProductLeasing(Fixture.Read("euronics-product-twenty.html"), new Uri("https://www.euronics.lv/p/1"));

        Assert.NotNull(terms);
        Assert.Equal(20m, terms.InterestRatePercent);
        Assert.False(terms.IsInterestFree);
    }

    [Fact]
    public void Product_page_without_calculator_returns_null() =>
        Assert.Null(EuronicsParser.ParseProductLeasing("<html><body>nav</body></html>", new Uri("https://www.euronics.lv/p/1")));
}

public class M79ParserTests
{
    private static readonly Uri Page = new("https://www.m79.lv/c/category/Ledusskapji");

    [Fact]
    public void Listing_parses_cards_once_each()
    {
        var items = M79Parser.ParseListing(Fixture.Read("m79-listing.html"), Page, ["www.m79.lv"], ["pictures.m79.lv"]);

        Assert.Equal(3, items.Count);
        var first = items[0];
        Assert.Equal("1305692", first.StoreProductId);
        Assert.Equal(538.02m, first.Price);
        Assert.Equal(599.00m, first.OldPrice);
        Assert.Equal("SAMSUNG", first.Brand);
        Assert.EndsWith("…", first.Name);
        Assert.Equal(1249m, items[1].Price);
        Assert.Equal("https://pictures.m79.lv/img/products/original/a.webp", first.ImageFallbackUrl);
    }

    [Theory]
    [InlineData("https://www.m79.lv/p/products/1/Ledusskapji/Bosch/X", "https://www.m79.lv/c/category/Ledusskapji", true)]
    [InlineData("https://www.m79.lv/p/products/1/Laptopi=portativie+datori/HP/X", "https://www.m79.lv/c/category/Laptopi=portativie+datori", true)]
    [InlineData("https://www.m79.lv/p/products/1/Roboti-puteklsuceji/X/Y", "https://www.m79.lv/c/category/Majas+kinozales", false)]
    public void Products_from_other_categories_are_rejected(string product, string listing, bool expected) =>
        Assert.Equal(expected, M79Parser.BelongsToListing(product, listing));

    [Fact]
    public void Policy_is_parsed_with_term_and_amount_range()
    {
        var policy = M79Parser.ParseStorePolicy(Fixture.Read("m79-policy.html"), new Uri("https://www.m79.lv/pages/bezprocentu-kredits"));

        Assert.NotNull(policy);
        Assert.True(policy.IsInterestFree);
        Assert.Equal(3, policy.MaxTermMonths);
        Assert.Equal(100m, policy.MinAmount);
        Assert.Equal(20000m, policy.MaxAmount);
        Assert.Equal(OfferScope.StoreWide, policy.Scope);
        Assert.False(policy.AppliesToPrice(89m));
        Assert.True(policy.AppliesToPrice(538.02m));
    }

    [Fact]
    public void Policy_without_zero_rate_returns_null() =>
        Assert.Null(M79Parser.ParseStorePolicy(Fixture.Read("m79-policy-ended.html"), new Uri("https://www.m79.lv/pages/bezprocentu-kredits")));
}

public class TetParserTests
{
    private static readonly Uri Page = new("https://www.tet.lv/veikals/televizori.html");

    [Fact]
    public void Listing_reads_pricing_json_and_product_specific_offer()
    {
        var items = TetParser.ParseListing(Fixture.Read("tet-listing.html"), Page, ["www.tet.lv"], ["cdn.tet.lv"]);

        Assert.Equal(3, items.Count); // foreign-host card and the "discount products" row are dropped
        Assert.DoesNotContain(items, i => i.StoreProductId == "55555");
        var tv = items.Single(i => i.StoreProductId == "99004");
        Assert.Equal(485m, tv.Price);
        Assert.Equal(576m, tv.OldPrice);
        Assert.Equal("Test Smart TV 50\"", tv.Name);
        Assert.Null(tv.ProductOffer);

        var promo = items.Single(i => i.StoreProductId == "99005");
        Assert.NotNull(promo.ProductOffer);
        Assert.Equal(24, promo.ProductOffer.MaxTermMonths);
        Assert.Equal(OfferScope.Product, promo.ProductOffer.Scope);
    }

    [Theory]
    [InlineData("[{\"term\":12,\"interest_rate\":5}]")]
    [InlineData("[{\"foo\":1}]")]
    [InlineData("{\"term\":12,\"rate\":0}")]
    [InlineData("not json")]
    public void Split_payment_without_clear_zero_rate_is_ignored(string json) =>
        Assert.Null(TetParser.ParseSplitPaymentOffer(json, "https://www.tet.lv/veikals/x.html"));

    [Fact]
    public void Policy_reads_terms_and_range()
    {
        var policy = TetParser.ParseStorePolicy(Fixture.Read("tet-policy.html"), new Uri("https://www.tet.lv/veikals/nomaksa"));

        Assert.NotNull(policy);
        Assert.Equal([3, 6, 12], policy.TermsMonths);
        Assert.Equal(12, policy.MaxTermMonths);
        Assert.Equal(70m, policy.MinAmount);
        Assert.Equal(10000m, policy.MaxAmount);
        Assert.Contains("GPL", policy.Conditions);
    }

    [Fact]
    public void Policy_without_interest_free_text_returns_null() =>
        Assert.Null(TetParser.ParseStorePolicy("<html><body>Nomaksa līdz 36 mēnešiem</body></html>", new Uri("https://www.tet.lv/veikals/nomaksa")));
}
