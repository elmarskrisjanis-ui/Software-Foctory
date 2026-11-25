using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using BezProcentu.Api;
using BezProcentu.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o =>
{
    o.IncludeScopes = true;
    o.UseUtcTimestamp = true;
    o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddSingleton(new OfferFreshness(builder.Configuration.GetValue("Offers:MaxAge", TimeSpan.FromHours(36))));
builder.Services.AddScoped<ProductQueries>();

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.TraceIdentifier;
    ctx.ProblemDetails.Detail = ctx.ProblemDetails.Status >= 500 ? null : ctx.ProblemDetails.Detail;
});

builder.Services.AddOpenApi("v1", o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "BezProcentu API";
    doc.Info.Version = "v1";
    doc.Info.Description = "Public, read-only API listing products from Riga online stores that currently offer interest-free (0%) financing. No authentication by design (see specification).";
    return Task.CompletedTask;
}));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("public", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimit:PermitPerMinute", 300),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
    foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
    {
        o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
});

var app = builder.Build();

app.UseForwardedHeaders();

// Correlation id: accept a well-formed incoming id, otherwise use our own; echo it back and add it to log scopes.
var correlationIdFormat = new Regex("^[A-Za-z0-9._:-]{8,64}$", RegexOptions.Compiled);
app.Use(async (ctx, next) =>
{
    var incoming = ctx.Request.Headers["X-Correlation-ID"].ToString();
    var id = correlationIdFormat.IsMatch(incoming) ? incoming : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    ctx.TraceIdentifier = id;
    ctx.Response.Headers["X-Correlation-ID"] = id;
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    ctx.Response.Headers["Cache-Control"] = "no-store";

    var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Request");
    using var scope = logger.BeginScope(new Dictionary<string, object> { ["correlationId"] = id });
    var started = Stopwatch.GetTimestamp();
    await next();
    logger.LogInformation("{Method} {Path} -> {Status} in {ElapsedMs:0} ms",
        ctx.Request.Method, ctx.Request.Path.Value, ctx.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
});

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = _ => StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseRateLimiter();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapApi();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

app.Run();

namespace BezProcentu.Api
{
    /// <summary>Marker type for integration tests (WebApplicationFactory).</summary>
    public sealed class ApiAssemblyMarker;
}
