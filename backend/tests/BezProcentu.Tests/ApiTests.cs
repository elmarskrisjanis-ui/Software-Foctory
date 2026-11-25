using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BezProcentu.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BezProcentu.Tests;

[Collection(DatabaseCollection.Name)]
public class ApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private WebApplicationFactory<BezProcentu.Api.ApiAssemblyMarker> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await fx.ResetAsync();
        await using (var db = fx.CreateContext())
        {
            await new OfferWriter(db, TimeProvider.System).SaveAsync("m79",
                [DatabaseTests.Product("1", 300, "televizori", "Samsung TV"), DatabaseTests.Product("2", 900, "monitori", "Asus monitors")], true, default);
        }

        _factory = new WebApplicationFactory<BezProcentu.Api.ApiAssemblyMarker>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("ConnectionStrings:Default", fx.ConnectionString);
        });
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Products_are_listed_without_authentication()
    {
        var response = await _client.GetAsync("/api/products?group=elektrotehnika");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, page.GetProperty("total").GetInt32());
        var item = page.GetProperty("items")[0];
        Assert.Equal("Samsung TV 1", item.GetProperty("name").GetString());
        Assert.Equal("M79", item.GetProperty("storeName").GetString());
        Assert.Equal("store", item.GetProperty("offerScope").GetString());
        Assert.Equal(100m, item.GetProperty("monthlyPayment").GetDecimal());
    }

    [Fact]
    public async Task Invalid_query_returns_problem_details_without_internals()
    {
        var response = await _client.GetAsync("/api/products?sort=evil&pageSize=9999");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"sort\"", body);
        Assert.Contains("\"pageSize\"", body);
        Assert.DoesNotContain("   at ", body); // no stack trace
    }

    [Fact]
    public async Task Unknown_product_returns_404()
    {
        var response = await _client.GetAsync("/api/products/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Security_and_correlation_headers_are_set()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog");
        request.Headers.Add("X-Correlation-ID", "test-correlation-123");
        var response = await _client.SendAsync(request);

        Assert.Equal("test-correlation-123", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task Malformed_correlation_id_is_replaced()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog");
        request.Headers.Add("X-Correlation-ID", "<script>");
        var response = await _client.SendAsync(request);
        Assert.NotEqual("<script>", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task Stores_endpoint_lists_unavailable_stores_and_offer_counts()
    {
        var stores = await _client.GetFromJsonAsync<JsonElement>("/api/stores");
        var list = stores.EnumerateArray().ToList();

        Assert.Contains(list, s => s.GetProperty("slug").GetString() == "1a" && s.GetProperty("status").GetString() == "unavailable");
        Assert.Equal(2, list.Single(s => s.GetProperty("slug").GetString() == "m79").GetProperty("activeOffers").GetInt32());
    }

    [Fact]
    public async Task Facets_and_catalog_are_available()
    {
        var facets = await _client.GetFromJsonAsync<JsonElement>("/api/facets");
        Assert.Equal(2, facets.GetProperty("total").GetInt32());

        var catalog = await _client.GetFromJsonAsync<JsonElement>("/api/catalog");
        Assert.Equal(3, catalog.GetProperty("groups").GetArrayLength());
    }

    [Fact]
    public async Task OpenApi_document_describes_public_endpoints()
    {
        var doc = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = doc.GetProperty("paths");
        foreach (var path in new[] { "/api/products", "/api/products/{id}", "/api/facets", "/api/catalog", "/api/stores" })
        {
            Assert.True(paths.TryGetProperty(path, out _), $"missing {path}");
        }
    }

    [Fact]
    public async Task Health_checks_report_ready()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Write_methods_are_not_allowed()
    {
        var response = await _client.PostAsync("/api/products", JsonContent.Create(new { name = "x" }));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
