using System.Net;
using BezProcentu.Collector.Collectors;
using BezProcentu.Collector.Http;
using BezProcentu.Core.Scraping;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BezProcentu.Tests;

/// <summary>Serves canned responses by absolute URL; records requests. No real network access in tests.</summary>
internal sealed class FakeStoreHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _routes = new(StringComparer.Ordinal);
    public List<HttpRequestMessage> Requests { get; } = [];

    public FakeStoreHandler Map(string url, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes[url] = (status, body);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var (status, body) = _routes.TryGetValue(request.RequestUri!.AbsoluteUri, out var r) ? r : (HttpStatusCode.NotFound, "");
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}

internal static class CollectorFactory
{
    public static IOptions<CollectorOptions> Options(int maxPages = 3) => Microsoft.Extensions.Options.Options.Create(new CollectorOptions
    {
        RequestDelay = TimeSpan.Zero,
        MaxPagesPerListing = maxPages,
    });

    public static PoliteFetcher Fetcher(FakeStoreHandler handler, IOptions<CollectorOptions>? options = null) =>
        new(new HttpClient(handler), options ?? Options(), TimeProvider.System, NullLogger<PoliteFetcher>.Instance);
}

public class RobotsTxtTests
{
    private const string Content = """
        User-agent: SemrushBot
        Disallow: /

        User-agent: *
        Disallow: /meklesana/
        Disallow: /*utm_source=*
        Allow: /meklesana/atlauts
        Disallow:
        """;

    [Theory]
    [InlineData("https://x.lv/tv/televizori", true)]
    [InlineData("https://x.lv/meklesana/tv", false)]
    [InlineData("https://x.lv/meklesana/atlauts", true)]
    [InlineData("https://x.lv/tv?utm_source=a", false)]
    public void Generic_rules_apply(string url, bool allowed) =>
        Assert.Equal(allowed, RobotsTxt.Parse(Content, "BezProcentuBot").IsAllowed(new Uri(url)));

    [Fact]
    public void Specific_agent_group_overrides_generic()
    {
        var robots = RobotsTxt.Parse("User-agent: BezProcentuBot\nDisallow: /\n\nUser-agent: *\nDisallow:", "BezProcentuBot");
        Assert.False(robots.IsAllowed(new Uri("https://x.lv/anything")));
    }
}

public class PoliteFetcherTests
{
    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "")]
    [InlineData(HttpStatusCode.TooManyRequests, "")]
    [InlineData(HttpStatusCode.OK, "<html><head><title>Just a moment...</title></head></html>")]
    public void Bot_challenges_are_detected(HttpStatusCode status, string body) =>
        Assert.True(PoliteFetcher.LooksLikeBotChallenge(status, body));

    [Fact]
    public void Normal_page_is_not_a_challenge() =>
        Assert.False(PoliteFetcher.LooksLikeBotChallenge(HttpStatusCode.OK, "<html><title>Televizori</title></html>"));

    [Fact]
    public async Task Challenge_page_stops_collection_instead_of_retrying()
    {
        var handler = new FakeStoreHandler()
            .Map("https://shop.example/robots.txt", "User-agent: *\nDisallow:")
            .Map("https://shop.example/tv", Fixture.Read("cloudflare.html"), HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<StoreBlockedException>(() => CollectorFactory.Fetcher(handler).GetStringAsync(new Uri("https://shop.example/tv"), CancellationToken.None));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Robots_disallow_prevents_request()
    {
        var handler = new FakeStoreHandler().Map("https://shop.example/robots.txt", "User-agent: *\nDisallow: /private");

        await Assert.ThrowsAsync<DisallowedByRobotsException>(() => CollectorFactory.Fetcher(handler).GetStringAsync(new Uri("https://shop.example/private/x"), CancellationToken.None));
        Assert.Single(handler.Requests); // only robots.txt was requested
    }

    [Fact]
    public async Task Requests_identify_the_bot()
    {
        var handler = new FakeStoreHandler()
            .Map("https://shop.example/robots.txt", "")
            .Map("https://shop.example/a", "<html></html>");

        await CollectorFactory.Fetcher(handler).GetStringAsync(new Uri("https://shop.example/a"), CancellationToken.None);
        Assert.Contains("BezProcentuBot", handler.Requests[^1].Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task Plain_http_is_refused()
    {
        var handler = new FakeStoreHandler();
        await Assert.ThrowsAsync<ArgumentException>(() => CollectorFactory.Fetcher(handler).GetStringAsync(new Uri("http://shop.example/a"), CancellationToken.None));
        Assert.Empty(handler.Requests);
    }
}

public class StoreCollectorTests
{
    private static FakeStoreHandler M79Site(string policyFixture)
    {
        var handler = new FakeStoreHandler()
            .Map("https://www.m79.lv/robots.txt", "User-agent: *\nDisallow: /app/")
            .Map("https://www.m79.lv/pages/bezprocentu-kredits", Fixture.Read(policyFixture))
            .Map("https://www.m79.lv/c/category/Ledusskapji?limit=96&page=1", Fixture.Read("m79-listing.html"))
            .Map("https://www.m79.lv/c/category/Ledusskapji?limit=96&page=2", "<html><body></body></html>");
        return handler;
    }

    [Fact]
    public async Task M79_store_wide_offer_applies_within_price_range()
    {
        var handler = M79Site("m79-policy.html");
        var collector = new M79Collector(CollectorFactory.Fetcher(handler), CollectorFactory.Options(), NullLogger<M79Collector>.Instance);

        var outcome = await collector.CollectAsync(CancellationToken.None);

        Assert.True(outcome.StorePolicyActive);
        var fridges = outcome.Products.Where(p => p.CategorySlug == "ledusskapji").ToList();
        Assert.Equal(2, fridges.Count); // 89 € product is below the 100 € minimum
        Assert.All(fridges, p => Assert.Equal(OfferScope.StoreWide, p.Offer.Scope));
        Assert.Equal(179.34m, fridges.Single(p => p.StoreProductId == "1305692").MonthlyPayment);
        Assert.False(outcome.Complete); // other configured listings are not mapped in the fake site
    }

    [Fact]
    public async Task M79_without_zero_rate_policy_returns_nothing()
    {
        var handler = M79Site("m79-policy-ended.html");
        var collector = new M79Collector(CollectorFactory.Fetcher(handler), CollectorFactory.Options(), NullLogger<M79Collector>.Instance);

        var outcome = await collector.CollectAsync(CancellationToken.None);

        Assert.False(outcome.StorePolicyActive);
        Assert.Empty(outcome.Products);
        Assert.DoesNotContain(handler.Requests, r => r.RequestUri!.AbsolutePath.StartsWith("/c/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Tet_uses_product_offer_first_and_store_offer_within_range()
    {
        var handler = new FakeStoreHandler()
            .Map("https://www.tet.lv/robots.txt", "User-agent: *\nDisallow: /administrator/")
            .Map("https://www.tet.lv/veikals/nomaksa", Fixture.Read("tet-policy.html"))
            .Map("https://www.tet.lv/veikals/televizori-un-audio/televizori-un-piederumi/televizori.html", Fixture.Read("tet-listing.html"))
            .Map("https://www.tet.lv/veikals/televizori-un-audio/televizori-un-piederumi/televizori.html?page=2", Fixture.Read("tet-listing.html"));
        var collector = new TetCollector(CollectorFactory.Fetcher(handler), CollectorFactory.Options(), NullLogger<TetCollector>.Instance);

        var outcome = await collector.CollectAsync(CancellationToken.None);

        var tvs = outcome.Products.Where(p => p.CategorySlug == "televizori").ToList();
        Assert.Equal(2, tvs.Count); // 9.99 € cable is below 70 €
        Assert.Equal(OfferScope.Product, tvs.Single(p => p.StoreProductId == "99005").Offer.Scope);
        Assert.Equal(OfferScope.StoreWide, tvs.Single(p => p.StoreProductId == "99004").Offer.Scope);
        Assert.Equal(12, tvs.Single(p => p.StoreProductId == "99004").Offer.MaxTermMonths);
    }
}
