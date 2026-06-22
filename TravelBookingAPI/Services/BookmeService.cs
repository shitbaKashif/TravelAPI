using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TravelBookingAPI.Models;

namespace TravelBookingAPI.Services;

public interface IBookmeService
{
    Task<(string RefID, List<BookmeItinerary> Itineraries)> SearchAsync(BookmeSearchRequest req, CancellationToken ct = default);
    Task<BookmeQuoteResponse> QuoteAsync(BookmeQuoteRequest req, CancellationToken ct = default);
    Task<BookmeReserveResponse> ReserveAsync(BookmeReserveRequest req, CancellationToken ct = default);
    Task<bool> IssueTicketAsync(string orderRefId, decimal amount, string partnerId, CancellationToken ct = default);
}

public class BookmeService : IBookmeService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BookmeService> _logger;

    private static readonly SemaphoreSlim _tokenLock = new(1, 1);
    private const string TokenCacheKey = "bookme_sanctum_token";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BookmeService(HttpClient http, IConfiguration config, IMemoryCache cache, ILogger<BookmeService> logger)
    {
        _http   = http;
        _config = config;
        _cache  = cache;
        _logger = logger;

        string baseUrl = (_config["Bookme:BaseUrl"] ?? "https://uat-api.bookmesky.com").TrimEnd('/') + "/";
        _http.BaseAddress = new Uri(baseUrl);
        _http.Timeout     = TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ── Token management ──────────────────────────────────────────────────────
    private async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<string>(TokenCacheKey, out var cached) && !string.IsNullOrEmpty(cached))
            return cached;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue<string>(TokenCacheKey, out cached) && !string.IsNullOrEmpty(cached))
                return cached;

            string username = _config["Bookme:Username"] ?? "";
            string password = _config["Bookme:Password"] ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Bookme credentials not configured — skipping auth.");
                return "";
            }

            var body = JsonSerializer.Serialize(new BookmeAuthRequest { Username = username, Password = password });
            using var req = new HttpRequestMessage(HttpMethod.Post, "partner/api/auth/token")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            using var resp = await _http.SendAsync(req, ct);
            string raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bookme auth failed: {Status} — {Body}", resp.StatusCode, raw[..Math.Min(300, raw.Length)]);
                return "";
            }

            var authResp = JsonSerializer.Deserialize<BookmeAuthResponse>(raw, JsonOpts);
            string token = authResp?.Token ?? "";

            if (!string.IsNullOrEmpty(token))
            {
                _cache.Set(TokenCacheKey, token, TimeSpan.FromMinutes(50));
                _logger.LogInformation("Bookme token acquired (cached 50 min).");
            }

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bookme token acquisition threw an exception.");
            return "";
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    // ── Generic authenticated call ────────────────────────────────────────────
    private async Task<T?> SendAuthenticatedAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct) where T : class
    {
        string token = await GetTokenAsync(ct);
        if (string.IsNullOrEmpty(token))
            return null;

        using var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body != null)
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        string raw = await resp.Content.ReadAsStringAsync(ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _cache.Remove(TokenCacheKey);
            _logger.LogWarning("Bookme 401 on {Path} — token cleared, will refresh on next call.", path);
            return null;
        }

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Bookme {Method} {Path} → {Status}: {Body}", method, path, (int)resp.StatusCode, raw[..Math.Min(500, raw.Length)]);
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(raw, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Bookme response from {Path}. Raw snippet: {Raw}", path, raw[..Math.Min(300, raw.Length)]);
            return null;
        }
    }

    // ── Search ────────────────────────────────────────────────────────────────
    public async Task<(string RefID, List<BookmeItinerary> Itineraries)> SearchAsync(BookmeSearchRequest req, CancellationToken ct = default)
    {
        try
        {
            var resp = await SendAuthenticatedAsync<BookmeSearchResponse>(HttpMethod.Post, "air/api/search", req, ct);
            if (resp == null)
                return ("", new List<BookmeItinerary>());

            _logger.LogInformation("Bookme search returned RefID={RefID} with {Count} itineraries.",
                resp.RefID, resp.GetItineraries().Count);

            return (resp.RefID, resp.GetItineraries());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bookme SearchAsync threw an exception.");
            return ("", new List<BookmeItinerary>());
        }
    }

    // ── Quote ─────────────────────────────────────────────────────────────────
    public async Task<BookmeQuoteResponse> QuoteAsync(BookmeQuoteRequest req, CancellationToken ct = default)
    {
        try
        {
            var resp = await SendAuthenticatedAsync<BookmeQuoteResponse>(HttpMethod.Post, "air/api/quote", req, ct);
            if (resp == null)
            {
                _logger.LogWarning("Bookme quote returned null for RefID={RefID}", req.RefID);
                return new BookmeQuoteResponse();
            }

            _logger.LogInformation("Bookme quote succeeded — CartRefID={RefID} Total={Total} Currency={Currency}",
                resp.RefID, resp.GetTotal(), resp.Currency);
            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bookme QuoteAsync threw an exception.");
            return new BookmeQuoteResponse();
        }
    }

    // ── Reserve ───────────────────────────────────────────────────────────────
    public async Task<BookmeReserveResponse> ReserveAsync(BookmeReserveRequest req, CancellationToken ct = default)
    {
        try
        {
            var resp = await SendAuthenticatedAsync<BookmeReserveResponse>(HttpMethod.Post, "air/api/reserve", req, ct);
            if (resp == null)
            {
                _logger.LogWarning("Bookme reserve returned null for RefID={RefID}", req.RefID);
                return new BookmeReserveResponse();
            }

            _logger.LogInformation("Bookme reserve succeeded — OrderRefId={OrderRefId} Status={Status}",
                resp.GetOrderRefId(), resp.Status);
            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bookme ReserveAsync threw an exception.");
            return new BookmeReserveResponse();
        }
    }

    // ── IPN (e-ticket issuance) ───────────────────────────────────────────────
    public async Task<bool> IssueTicketAsync(string orderRefId, decimal amount, string partnerId, CancellationToken ct = default)
    {
        string ipnBaseUrl   = _config["Bookme:IpnBaseUrl"]   ?? "https://api-uat.bookme.pk";
        string apiKey       = _config["Bookme:IpnApiKey"]    ?? "";
        string authHeader   = _config["Bookme:IpnAuthHeader"] ?? "";

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(authHeader))
        {
            _logger.LogWarning("Bookme IPN credentials not configured — skipping e-ticket for OrderRefId={OrderRefId}.", orderRefId);
            return false;
        }

        var ipnBody = new BookmeIpnRequest
        {
            ApiKey    = apiKey,
            OrderRefId = orderRefId,
            Type      = "airline",
            Status    = "confirm",
            Amount    = amount,
            PartnerId = partnerId
        };

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ipnBaseUrl.TrimEnd('/')}/api/v2/partners/ipn");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(ipnBody), Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, ct);
            string raw = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("IPN e-ticket issued for OrderRefId={OrderRefId} PartnerId={PartnerId}: {Response}",
                    orderRefId, partnerId, raw[..Math.Min(200, raw.Length)]);
                return true;
            }

            _logger.LogWarning("IPN e-ticket failed for OrderRefId={OrderRefId}: {Status} {Body}",
                orderRefId, (int)response.StatusCode, raw[..Math.Min(300, raw.Length)]);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IPN IssueTicketAsync threw for OrderRefId={OrderRefId}", orderRefId);
            return false;
        }
    }
}
