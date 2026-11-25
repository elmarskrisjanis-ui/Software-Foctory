using BezProcentu.Core.Scraping;
using BezProcentu.Data;
using Microsoft.EntityFrameworkCore;

namespace BezProcentu.Tests;

[Collection(DatabaseCollection.Name)]
public class DatabaseTests(DatabaseFixture fx) : IAsyncLifetime
{
    private static readonly OfferFreshness Freshness = new(TimeSpan.FromHours(36));

    public Task InitializeAsync() => fx.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    internal static CollectedProduct Product(string id, decimal price, string category = "televizori", string name = "Prece", int term = 3) =>
        new(id, $"{name} {id}", $"https://www.m79.lv/p/products/{id}/x", null, price, null, "Brand", category,
            new LeasingTerms(OfferScope.StoreWide, 0m, term, [term], 100m, 20000m, "Aizdevums.lv", "0% uz 3 mēn.", "https://www.m79.lv/pages/bezprocentu-kredits"));

    [Fact]
    public async Task Complete_run_deactivates_products_no_longer_offered()
    {
        await using (var db = fx.CreateContext())
        {
            await new OfferWriter(db, TimeProvider.System).SaveAsync("m79", [Product("1", 300), Product("2", 600)], complete: true, default);
        }

        await using (var db = fx.CreateContext())
        {
            await new OfferWriter(db, TimeProvider.System).SaveAsync("m79", [Product("1", 290)], complete: true, default);
        }

        await using var check = fx.CreateContext();
        var rows = await check.Products.OrderBy(p => p.StoreProductId).ToListAsync();
        Assert.True(rows[0].IsOfferActive);
        Assert.Equal(290m, rows[0].Price);
        Assert.Equal(96.67m, rows[0].MonthlyPayment);
        Assert.False(rows[1].IsOfferActive);
    }

    [Fact]
    public async Task Partial_run_keeps_unseen_products_until_they_become_stale()
    {
        await using (var db = fx.CreateContext())
        {
            var w = new OfferWriter(db, TimeProvider.System);
            await w.SaveAsync("m79", [Product("1", 300), Product("2", 600)], complete: true, default);
            await w.SaveAsync("m79", [Product("1", 300)], complete: false, default);
        }

        await using var check = fx.CreateContext();
        Assert.Equal(2, await check.Products.CountAsync(p => p.IsOfferActive));
    }

    [Fact]
    public async Task Stale_and_inactive_offers_are_not_visible()
    {
        await using (var db = fx.CreateContext())
        {
            await new OfferWriter(db, TimeProvider.System).SaveAsync("m79", [Product("fresh", 300), Product("stale", 300), Product("off", 300)], true, default);
            await db.Products.Where(p => p.StoreProductId == "stale")
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.OfferVerifiedAt, DateTimeOffset.UtcNow.AddHours(-40)));
            await db.Products.Where(p => p.StoreProductId == "off")
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsOfferActive, false));
        }

        await using var read = fx.CreateContext();
        var page = await new ProductQueries(read, TimeProvider.System, Freshness)
            .SearchAsync(new ProductQuery(null, null, null, null, null, null, ProductSort.MonthlyAsc, 1, 24), default);

        Assert.Equal(["fresh"], page.Items.Select(i => i.Name.Split(' ')[^1]));
    }

    [Fact]
    public async Task Store_deactivation_hides_all_store_offers()
    {
        await using var db = fx.CreateContext();
        var w = new OfferWriter(db, TimeProvider.System);
        await w.SaveAsync("m79", [Product("1", 300), Product("2", 400)], true, default);
        await w.SaveAsync("tet", [Product("9", 300)], true, default);

        Assert.Equal(2, await w.DeactivateStoreAsync("m79", default));
        Assert.Equal(1, await db.Products.CountAsync(p => p.IsOfferActive));
    }

    [Fact]
    public async Task Filters_search_and_sorting_work()
    {
        await using (var db = fx.CreateContext())
        {
            await new OfferWriter(db, TimeProvider.System).SaveAsync("m79",
            [
                Product("1", 300, "televizori", "Samsung TV"),
                Product("2", 900, "monitori", "Asus monitors"),
                Product("3", 1200, "ledusskapji", "Bosch 100% ledusskapis"),
                Product("4", 150, "monitori", "Dell monitors"),
            ], true, default);
        }

        await using var read = fx.CreateContext();
        var q = new ProductQueries(read, TimeProvider.System, Freshness);
        ProductQuery Q(string? group = null, string? category = null, string? search = null, decimal? min = null, ProductSort sort = ProductSort.MonthlyAsc) =>
            new(group, category, null, search, min, null, sort, 1, 24);

        Assert.Equal(2, (await q.SearchAsync(Q(group: "datortehnika"), default)).Total); // only the two monitors
        Assert.Equal(2, (await q.SearchAsync(Q(category: "monitori"), default)).Total);
        Assert.Single((await q.SearchAsync(Q(search: "samsung"), default)).Items);
        Assert.Single((await q.SearchAsync(Q(search: "100%"), default)).Items); // % is matched literally
        Assert.Single((await q.SearchAsync(Q(search: "%"), default)).Items); // a lone % is not a wildcard
        Assert.Equal([1200m, 900m, 300m, 150m], (await q.SearchAsync(Q(sort: ProductSort.PriceDesc), default)).Items.Select(i => i.Price));
        Assert.Equal(2, (await q.SearchAsync(Q(min: 500), default)).Total);

        var facets = await q.FacetsAsync(Q(), default);
        Assert.Equal(4, facets.Total);
        Assert.Equal(2, facets.Categories.Single(c => c.Slug == "monitori").Count);
        Assert.Equal(4, facets.Stores.Single(s => s.Slug == "m79").Count);
    }
}
