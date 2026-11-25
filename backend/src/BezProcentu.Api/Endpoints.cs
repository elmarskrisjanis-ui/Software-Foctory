using BezProcentu.Core.Catalog;
using BezProcentu.Core.Stores;
using BezProcentu.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BezProcentu.Api;

public sealed record CatalogResponse(IReadOnlyList<CategoryGroup> Groups, IReadOnlyList<Category> Categories, IReadOnlyList<string> SortOptions);

public sealed record StoreStatusDto(
    string Slug,
    string Name,
    string HomepageUrl,
    string Status,
    string? Note,
    DateTimeOffset? LastSuccessfulRunAt,
    string? LastRunStatus,
    DateTimeOffset? LastRunAt,
    bool? InterestFreeProgrammeActive,
    int ActiveOffers);

/// <summary>
/// Public, read-only endpoints. The specification requires the site to work without authentication, so no
/// endpoint here exposes anything except collected public store data and collection status.
/// </summary>
public static class Endpoints
{
    public static void MapApi(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireRateLimiting("public").WithTags("Public");

        api.MapGet("/products", SearchProducts)
            .WithName("SearchProducts")
            .WithSummary("Products that currently have an interest-free (0%) offer.");

        api.MapGet("/products/{id:long}", GetProduct)
            .WithName("GetProduct")
            .WithSummary("One product with an active 0% offer.");

        api.MapGet("/facets", GetFacets)
            .WithName("GetFacets")
            .WithSummary("Counts per group, category and store for the given filters.");

        api.MapGet("/catalog", () => TypedResults.Ok(new CatalogResponse(ProductCatalog.Groups, ProductCatalog.Categories, ProductQueryValidator.SortValues.ToList())))
            .WithName("GetCatalog")
            .WithSummary("Product groups, categories and sort options.");

        api.MapGet("/stores", GetStores)
            .WithName("GetStores")
            .WithSummary("Supported stores and the freshness of their data.");
    }

    private static async Task<Results<Ok<ProductPage>, ValidationProblem>> SearchProducts(
        [AsParameters] ProductQueryInput input, ProductQueries queries, CancellationToken ct)
    {
        var (query, errors) = ProductQueryValidator.Validate(input);
        return query is null ? TypedResults.ValidationProblem(errors) : TypedResults.Ok(await queries.SearchAsync(query, ct));
    }

    private static async Task<Results<Ok<ProductDto>, NotFound<ProblemDetails>>> GetProduct(long id, ProductQueries queries, CancellationToken ct)
    {
        var product = id > 0 ? await queries.GetAsync(id, ct) : null;
        return product is null
            ? TypedResults.NotFound(new ProblemDetails { Title = "Product not found or offer no longer active.", Status = 404 })
            : TypedResults.Ok(product);
    }

    private static async Task<Results<Ok<Facets>, ValidationProblem>> GetFacets(
        [AsParameters] ProductQueryInput input, ProductQueries queries, CancellationToken ct)
    {
        var (query, errors) = ProductQueryValidator.Validate(input);
        return query is null ? TypedResults.ValidationProblem(errors) : TypedResults.Ok(await queries.FacetsAsync(query, ct));
    }

    private static async Task<Ok<List<StoreStatusDto>>> GetStores(AppDbContext db, ProductQueries queries, CancellationToken ct)
    {
        var runs = await db.CollectionRuns.AsNoTracking()
            .Where(r => r.FinishedAt != null)
            .GroupBy(r => r.StoreSlug)
            .Select(g => new
            {
                Store = g.Key,
                Last = g.OrderByDescending(r => r.StartedAt).First(),
                LastOk = g.Where(r => r.Status == RunStatus.Succeeded || r.Status == RunStatus.PartiallySucceeded)
                    .Max(r => (DateTimeOffset?)r.FinishedAt),
            })
            .ToDictionaryAsync(x => x.Store, ct);

        var offers = await queries.Visible().GroupBy(p => p.StoreSlug)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var result = StoreSources.All.Select(s =>
        {
            runs.TryGetValue(s.Slug, out var r);
            return new StoreStatusDto(
                s.Slug,
                s.Name,
                s.HomepageUrl,
                s.Status == StoreStatus.Active ? "active" : "unavailable",
                s.StatusNote,
                r?.LastOk,
                r?.Last.Status.ToString().ToLowerInvariant(),
                r?.Last.FinishedAt,
                r?.Last.StorePolicyActive,
                offers.GetValueOrDefault(s.Slug));
        }).ToList();

        return TypedResults.Ok(result);
    }
}
