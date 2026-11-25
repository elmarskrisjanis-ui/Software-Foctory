using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

namespace BezProcentu.Collector.Http;

public sealed class CollectorOptions
{
    public const string Section = "Collector";

    /// <summary>How often each store is refreshed. Must be at most 24h so data is updated at least daily.</summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(12);

    public TimeSpan SchedulerTick { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Minimum pause between two requests to the same host.</summary>
    public TimeSpan RequestDelay { get; set; } = TimeSpan.FromSeconds(1.5);

    public int MaxPagesPerListing { get; set; } = 10;

    /// <summary>Cap for stores that need one request per product page (Euronics).</summary>
    public int MaxProductPagesPerRun { get; set; } = 1500;

    public int MaxResponseBytes { get; set; } = 6 * 1024 * 1024;

    public string UserAgent { get; set; } = "BezProcentuBot/1.0 (personal, non-commercial; respects robots.txt)";

    public string RobotsAgentToken { get; set; } = "BezProcentuBot";

    /// <summary>Optional: only collect these store slugs (empty = all active stores).</summary>
    public string[] Stores { get; set; } = [];
}

/// <summary>The store answered with a bot challenge or access denial. We stop instead of trying to get around it.</summary>
public sealed class StoreBlockedException(string message) : Exception(message);

public sealed class DisallowedByRobotsException(Uri uri) : Exception($"robots.txt disallows {uri.AbsolutePath}");

/// <summary>
/// HTTP fetching with a fixed identifying User-Agent, robots.txt checks, a per-host delay and a response size cap.
/// TLS certificate validation stays enabled (default HttpClient behaviour).
/// </summary>
public sealed class PoliteFetcher(HttpClient http, IOptions<CollectorOptions> options, TimeProvider time, ILogger<PoliteFetcher> log)
{
    private readonly CollectorOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, RobotsTxt> _robots = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastRequest = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<string?> GetStringAsync(Uri uri, CancellationToken ct)
    {
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only https URLs are fetched.", nameof(uri));
        }

        var robots = await GetRobotsAsync(uri, ct);
        if (!robots.IsAllowed(uri))
        {
            throw new DisallowedByRobotsException(uri);
        }

        var (status, body) = await SendAsync(uri, ct);
        if (status == HttpStatusCode.NotFound || status == HttpStatusCode.Gone)
        {
            return null;
        }

        if (LooksLikeBotChallenge(status, body))
        {
            throw new StoreBlockedException($"{uri.Host} answered with a bot challenge or access denial (HTTP {(int)status}).");
        }

        if ((int)status >= 400)
        {
            throw new HttpRequestException($"HTTP {(int)status} for {uri.Host}{uri.AbsolutePath}", null, status);
        }

        return body;
    }

    internal static bool LooksLikeBotChallenge(HttpStatusCode status, string body)
    {
        if (status is HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable or (HttpStatusCode)429)
        {
            return true;
        }

        var head = body.Length > 4000 ? body[..4000] : body;
        return head.Contains("<title>Just a moment...</title>", StringComparison.OrdinalIgnoreCase)
            || head.Contains("cf-challenge", StringComparison.OrdinalIgnoreCase)
            || head.Contains("challenge-platform", StringComparison.OrdinalIgnoreCase)
            || head.Contains("Attention Required! | Cloudflare", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<RobotsTxt> GetRobotsAsync(Uri uri, CancellationToken ct)
    {
        var key = uri.GetLeftPart(UriPartial.Authority);
        if (_robots.TryGetValue(key, out var cached))
        {
            return cached;
        }

        RobotsTxt robots;
        try
        {
            var (status, body) = await SendAsync(new Uri(key + "/robots.txt"), ct);
            robots = (int)status switch
            {
                >= 200 and < 300 => RobotsTxt.Parse(body, _options.RobotsAgentToken),
                >= 400 and < 500 when status != HttpStatusCode.Forbidden && status != (HttpStatusCode)429 => RobotsTxt.AllowAll,
                _ => throw new StoreBlockedException($"robots.txt of {uri.Host} returned HTTP {(int)status}; not crawling."),
            };
        }
        catch (HttpRequestException ex)
        {
            throw new StoreBlockedException($"robots.txt of {uri.Host} could not be read: {ex.Message}");
        }

        _robots[key] = robots;
        return robots;
    }

    private async Task<(HttpStatusCode Status, string Body)> SendAsync(Uri uri, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_lastRequest.TryGetValue(uri.Host, out var last))
            {
                var wait = last + _options.RequestDelay - time.GetUtcNow();
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, time, ct);
                }
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,text/plain;q=0.9");
            request.Headers.AcceptLanguage.ParseAdd("lv,en;q=0.5");

            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            _lastRequest[uri.Host] = time.GetUtcNow();

            if (response.Content.Headers.ContentLength > _options.MaxResponseBytes)
            {
                throw new HttpRequestException($"Response from {uri.Host} is larger than the allowed limit.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + read > _options.MaxResponseBytes)
                {
                    throw new HttpRequestException($"Response from {uri.Host} is larger than the allowed limit.");
                }

                buffer.Write(chunk, 0, read);
            }

            log.LogDebug("GET {Host}{Path} -> {Status} ({Bytes} B)", uri.Host, uri.AbsolutePath, (int)response.StatusCode, buffer.Length);
            return (response.StatusCode, Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
        }
        finally
        {
            _gate.Release();
        }
    }
}
