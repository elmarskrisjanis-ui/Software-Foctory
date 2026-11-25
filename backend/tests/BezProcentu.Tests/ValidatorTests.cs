using BezProcentu.Api;
using BezProcentu.Data;

namespace BezProcentu.Tests;

public class ValidatorTests
{
    private static ProductQueryInput Input(string? group = null, string? category = null, string? store = null, string? q = null,
        string? min = null, string? max = null, string? sort = null, string? page = null, string? size = null) =>
        new(group, category, store, q, min, max, sort, page, size);

    [Fact]
    public void Defaults_are_applied()
    {
        var (query, errors) = ProductQueryValidator.Validate(Input());
        Assert.Empty(errors);
        Assert.Equal(new ProductQuery(null, null, null, null, null, null, ProductSort.MonthlyAsc, 1, 24), query);
    }

    [Fact]
    public void Valid_filters_are_accepted()
    {
        var (query, errors) = ProductQueryValidator.Validate(Input("datortehnika", "monitori", "m79", " asus ", "100", "500.5", "price-desc", "2", "12"));
        Assert.Empty(errors);
        Assert.Equal("asus", query!.Search);
        Assert.Equal(500.5m, query.MaxPrice);
        Assert.Equal(ProductSort.PriceDesc, query.Sort);
    }

    [Theory]
    [InlineData("group", "nav")]
    [InlineData("category", "../etc")]
    [InlineData("store", "1a")]
    [InlineData("minPrice", "-1")]
    [InlineData("maxPrice", "1e9")]
    [InlineData("sort", "name; DROP TABLE products")]
    [InlineData("page", "0")]
    [InlineData("pageSize", "1000")]
    public void Invalid_values_are_rejected(string field, string value)
    {
        var input = field switch
        {
            "group" => Input(group: value),
            "category" => Input(category: value),
            "store" => Input(store: value),
            "minPrice" => Input(min: value),
            "maxPrice" => Input(max: value),
            "sort" => Input(sort: value),
            "page" => Input(page: value),
            _ => Input(size: value),
        };

        var (query, errors) = ProductQueryValidator.Validate(input);
        Assert.Null(query);
        Assert.True(errors.ContainsKey(field));
    }

    [Fact]
    public void Too_long_search_is_rejected() =>
        Assert.True(ProductQueryValidator.Validate(Input(q: new string('a', 101))).Errors.ContainsKey("q"));

    [Fact]
    public void Min_greater_than_max_is_rejected() =>
        Assert.True(ProductQueryValidator.Validate(Input(min: "500", max: "100")).Errors.ContainsKey("maxPrice"));
}
