using ClosedXML.Excel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TravelBookingAPI.Models;

namespace TravelBookingAPI.Services;

public interface IFlightDataService
{
    Task<List<FlightResult>> SearchFlightsAsync(FlightSearchRequest req, CancellationToken cancellationToken = default);
    Task<List<CityInfo>> GetAllCitiesAsync(CancellationToken cancellationToken = default);
}

public class FlightDataService : IFlightDataService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<FlightDataService> _logger;
    private readonly string _excelPath;

    public FlightDataService(HttpClient httpClient, IConfiguration config, ILogger<FlightDataService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(config["ScrapingBee:BaseUrl"] ?? "https://app.scrapingbee.com");
        _excelPath = config["FlightDataPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Data", "FlightData.xlsx");
    }

    public async Task<List<FlightResult>> SearchFlightsAsync(FlightSearchRequest req, CancellationToken cancellationToken = default)
    {
        var excel = SearchFlightsFromExcel(req);

        if (!UseScrapingBee())
            return await Task.FromResult(excel);

        var webPrices = await FetchScrapingBeePricesOrderedAsync(req, cancellationToken);
        if (webPrices.Count == 0)
            return await Task.FromResult(excel);

        if (excel.Count == 0)
            return await Task.FromResult(BuildWebOnlyResults(req, webPrices));

        return await Task.FromResult(MergeWebPricesWithExcel(req, excel, webPrices));
    }

    public async Task<List<CityInfo>> GetAllCitiesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(GetCitiesFromExcel());
    }

    private string BuildScrapingBeeSearchQuery(FlightSearchRequest req)
    {
        var culture = CultureInfo.GetCultureInfo("en-GB");
        var sb = new StringBuilder();
        sb.Append("flights from ").Append(req.FromCity.Trim()).Append(" to ").Append(req.ToCity.Trim());
        sb.Append(" on ").Append(req.DepartureDate.ToString("d MMMM yyyy", culture));
        if (req.IsSameDay)
        {
            sb.Append(" same day return");
            if (!string.IsNullOrWhiteSpace(req.DepartureTime) && !string.IsNullOrWhiteSpace(req.ReturnTime))
                sb.Append(" outbound ").Append(req.DepartureTime.Trim()).Append(" return ").Append(req.ReturnTime.Trim());
        }
        else if (req.IsRoundTrip && req.ReturnDate.HasValue)
            sb.Append(" returning ").Append(req.ReturnDate.Value.ToString("d MMMM yyyy", culture));
        else
            sb.Append(" one way");
        sb.Append(' ').Append(req.CabinClass).Append(" class");
        sb.Append(" passengers ").Append(Math.Clamp(req.Passengers, 1, 9));
        if (!string.IsNullOrWhiteSpace(req.TravelPurpose))
            sb.Append(" purpose ").Append(req.TravelPurpose.Trim());
        if (!string.IsNullOrWhiteSpace(req.PurposeDetail))
            sb.Append(" ").Append(req.PurposeDetail.Trim());
        if (!string.IsNullOrWhiteSpace(req.PreferredAirline))
            sb.Append(" airline ").Append(req.PreferredAirline.Trim());
        sb.Append(" Pakistan PKR");
        return sb.ToString();
    }

    private async Task<List<decimal>> FetchScrapingBeePricesOrderedAsync(FlightSearchRequest req, CancellationToken cancellationToken)
    {
        var prices = new List<decimal>();
        string? apiKey = _config["ScrapingBee:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return prices;

        string searchQuery = BuildScrapingBeeSearchQuery(req);
        string countryCode = _config["ScrapingBee:CountryCode"] ?? "pk";
        string endpoint = $"/api/v1/fast_search?api_key={Uri.EscapeDataString(apiKey)}&search={Uri.EscapeDataString(searchQuery)}&country_code={Uri.EscapeDataString(countryCode)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ScrapingBee fast_search failed: {Status}", response.StatusCode);
            return prices;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        string[] resultCollections = ["organic", "organic_results", "results", "items"];
        foreach (string collection in resultCollections)
        {
            if (!doc.RootElement.TryGetProperty(collection, out var items) || items.ValueKind != JsonValueKind.Array)
                continue;
            if (items.GetArrayLength() == 0)
                continue;

            foreach (var item in items.EnumerateArray().Take(20))
            {
                string title = item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
                string snippet =
                    item.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" :
                    item.TryGetProperty("snippet", out var snipEl) ? snipEl.GetString() ?? "" : "";
                string line = $"{title} {snippet}";
                decimal total = ExtractBestPkrTotal(line);
                if (total > 0)
                    prices.Add(total);
            }

            break;
        }

        return prices.OrderBy(p => p).ToList();
    }

    private List<FlightResult> MergeWebPricesWithExcel(FlightSearchRequest req, List<FlightResult> excelRows, List<decimal> webPrices)
    {
        string tripType = ResolveTripType(req);
        int pax = Math.Max(req.Passengers, 1);
        int n = Math.Min(excelRows.Count, webPrices.Count);
        var merged = new List<FlightResult>();

        for (int i = 0; i < n; i++)
        {
            var e = excelRows[i];
            decimal total = webPrices[i];
            merged.Add(new FlightResult
            {
                FlightId = e.FlightId,
                Airline = e.Airline,
                FromCity = e.FromCity,
                FromCode = e.FromCode,
                ToCity = e.ToCity,
                ToCode = e.ToCode,
                DepartureTime = e.DepartureTime,
                ArrivalTime = e.ArrivalTime,
                Duration = e.Duration,
                CabinClass = e.CabinClass,
                TripType = tripType,
                PricePerPerson = decimal.Round(total / pax, 2),
                TotalPrice = decimal.Round(total, 2),
                Currency = "PKR",
                FlightType = e.FlightType
            });
        }

        for (int i = n; i < excelRows.Count; i++)
        {
            var e = excelRows[i];
            merged.Add(new FlightResult
            {
                FlightId = e.FlightId,
                Airline = e.Airline,
                FromCity = e.FromCity,
                FromCode = e.FromCode,
                ToCity = e.ToCity,
                ToCode = e.ToCode,
                DepartureTime = e.DepartureTime,
                ArrivalTime = e.ArrivalTime,
                Duration = e.Duration,
                CabinClass = e.CabinClass,
                TripType = tripType,
                PricePerPerson = e.PricePerPerson,
                TotalPrice = e.TotalPrice,
                Currency = e.Currency,
                FlightType = e.FlightType
            });
        }

        return merged.OrderBy(f => f.TotalPrice).ToList();
    }

    private List<FlightResult> BuildWebOnlyResults(FlightSearchRequest req, List<decimal> webPrices)
    {
        string tripType = ResolveTripType(req);
        int pax = Math.Max(req.Passengers, 1);
        string fromCode = ResolveCityCode(req.FromCity);
        string toCode = ResolveCityCode(req.ToCity);
        string depTime = $"{req.DepartureDate:yyyy-MM-dd} {(string.IsNullOrWhiteSpace(req.DepartureTime) ? "09:00" : req.DepartureTime)}";
        string flightType = IsDomesticByText(req.FromCity, req.ToCity) ? "Domestic" : "International";

        var list = new List<FlightResult>();
        int id = 1;
        foreach (decimal total in webPrices.Take(20))
        {
            list.Add(new FlightResult
            {
                FlightId = $"WEB-{id:D4}",
                Airline = "Multiple carriers",
                FromCity = req.FromCity,
                FromCode = fromCode,
                ToCity = req.ToCity,
                ToCode = toCode,
                DepartureTime = depTime,
                ArrivalTime = string.Empty,
                Duration = "See provider",
                CabinClass = req.CabinClass,
                TripType = tripType,
                PricePerPerson = decimal.Round(total / pax, 2),
                TotalPrice = decimal.Round(total, 2),
                Currency = "PKR",
                FlightType = flightType
            });
            id++;
        }

        return list;
    }

    private static string ResolveTripType(FlightSearchRequest req)
    {
        if (req.IsSameDay)
            return "Same Day";
        return req.IsRoundTrip ? "Round-Trip" : "One-Way";
    }

    private List<FlightResult> SearchFlightsFromExcel(FlightSearchRequest req)
    {
        var results = new List<FlightResult>();
        if (!File.Exists(_excelPath))
            return results;

        string tripPriceCol = req.IsRoundTrip ? "RoundTrip" : "OneWay";
        string classPrefix = req.CabinClass.Equals("Business", StringComparison.OrdinalIgnoreCase) ? "Business" : "Economy";
        string targetCol = $"{classPrefix}_{tripPriceCol}";
        int id = 1;

        using var wb = new XLWorkbook(_excelPath);
        foreach (var ws in wb.Worksheets)
        {
            var range = ws.RangeUsed();
            if (range == null) continue;

            foreach (var row in range.RowsUsed().Skip(1))
            {
                string from = row.Cell(1).GetString().Trim();
                string fromCode = row.Cell(2).GetString().Trim();
                string to = row.Cell(3).GetString().Trim();
                string toCode = row.Cell(4).GetString().Trim();
                string airline = row.Cell(5).GetString().Trim();
                string duration = row.Cell(6).GetString().Trim();

                bool forward = MatchCityOrCode(from, fromCode, req.FromCity) && MatchCityOrCode(to, toCode, req.ToCity);
                bool reverse = MatchCityOrCode(from, fromCode, req.ToCity) && MatchCityOrCode(to, toCode, req.FromCity);
                if (!forward && !reverse) continue;

                if (!string.IsNullOrWhiteSpace(req.PreferredAirline) &&
                    !airline.Contains(req.PreferredAirline, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                decimal pricePerPerson = GetColumnValue(ws, row, targetCol);
                if (pricePerPerson <= 0) continue;

                string displayFrom = forward ? from : to;
                string displayFromCode = forward ? fromCode : toCode;
                string displayTo = forward ? to : from;
                string displayToCode = forward ? toCode : fromCode;
                string depTime = $"{req.DepartureDate:yyyy-MM-dd} {(string.IsNullOrWhiteSpace(req.DepartureTime) ? "09:00" : req.DepartureTime)}";

                results.Add(new FlightResult
                {
                    FlightId = $"EXL-{id:D4}",
                    Airline = airline,
                    FromCity = displayFrom,
                    FromCode = displayFromCode,
                    ToCity = displayTo,
                    ToCode = displayToCode,
                    DepartureTime = depTime,
                    ArrivalTime = AddDuration(depTime, duration),
                    Duration = duration,
                    CabinClass = req.CabinClass,
                    TripType = ResolveTripType(req),
                    PricePerPerson = pricePerPerson,
                    TotalPrice = pricePerPerson * req.Passengers,
                    Currency = "PKR",
                    FlightType = ws.Name
                });
                id++;
            }
        }

        return results.OrderBy(x => x.TotalPrice).ToList();
    }

    private List<CityInfo> GetCitiesFromExcel()
    {
        var result = new List<CityInfo>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_excelPath))
            return result;

        using var wb = new XLWorkbook(_excelPath);
        foreach (var ws in wb.Worksheets)
        {
            var range = ws.RangeUsed();
            if (range == null) continue;

            foreach (var row in range.RowsUsed().Skip(1))
            {
                AddCity(row.Cell(1).GetString(), row.Cell(2).GetString(), ws.Name);
                AddCity(row.Cell(3).GetString(), row.Cell(4).GetString(), ws.Name);
            }
        }

        return result.OrderBy(c => c.Type).ThenBy(c => c.Name).ToList();

        void AddCity(string name, string code, string type)
        {
            string normalizedCode = code.Trim();
            if (string.IsNullOrWhiteSpace(normalizedCode) || !seenCodes.Add(normalizedCode))
                return;

            result.Add(new CityInfo
            {
                Code = normalizedCode,
                Name = name.Trim(),
                Country = type.Equals("Domestic", StringComparison.OrdinalIgnoreCase) ? "Pakistan" : "International",
                Type = type
            });
        }
    }

    private bool UseScrapingBee() => !string.IsNullOrWhiteSpace(_config["ScrapingBee:ApiKey"]);

    private static bool IsDomesticByText(string origin, string destination)
    {
        var pkAirports = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "KHI", "LHE", "ISB", "PEW", "UET", "MUX", "LYP", "SKT"
        };

        string o = NormalizeToAirportCode(origin);
        string d = NormalizeToAirportCode(destination);
        return pkAirports.Contains(o) && pkAirports.Contains(d);
    }

    private static string NormalizeToAirportCode(string input)
    {
        string s = input.Trim();
        if (s.Length == 3 && s.All(char.IsLetter))
            return s.ToUpperInvariant();

        return s.ToLowerInvariant() switch
        {
            "karachi" => "KHI",
            "lahore" => "LHE",
            "islamabad" => "ISB",
            "peshawar" => "PEW",
            "quetta" => "UET",
            "multan" => "MUX",
            "faisalabad" => "LYP",
            _ => s.ToUpperInvariant()
        };
    }

    private static string ResolveCityCode(string input)
    {
        string code = NormalizeToAirportCode(input);
        return code.Length == 3 && code.All(char.IsLetter) ? code : input.Trim();
    }

    /// <summary>Lowest plausible PKR total in a search snippet (handles multiple prices per line).</summary>
    private static decimal ExtractBestPkrTotal(string text)
    {
        string normalized = text
            .Replace('\u00A0', ' ')
            .Replace('\u202F', ' ')
            .Replace('\u2009', ' ');

        decimal best = 0m;
        foreach (Match m in Regex.Matches(normalized, @"(?:PKR|Rs\.?)\s*([0-9][0-9,\.]*)", RegexOptions.IgnoreCase))
        {
            string raw = m.Groups[1].Value.Replace(",", "");
            if (!decimal.TryParse(raw, out var v) || v < 2_000m || v > 5_000_000m)
                continue;
            if (best == 0m || v < best)
                best = v;
        }

        return best;
    }

    private static bool MatchCityOrCode(string city, string code, string query)
    {
        return city.Contains(query, StringComparison.OrdinalIgnoreCase)
               || code.Equals(query, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal GetColumnValue(IXLWorksheet ws, IXLRangeRow row, string colName)
    {
        var header = ws.Row(1).CellsUsed()
            .FirstOrDefault(c => c.GetString().Equals(colName, StringComparison.OrdinalIgnoreCase));
        if (header == null) return 0m;

        var cell = row.Cell(header.Address.ColumnNumber);
        return cell.TryGetValue(out decimal value) ? value : 0m;
    }

    private static string AddDuration(string depTime, string duration)
    {
        if (!DateTime.TryParse(depTime, out var dt))
            return depTime;

        var parts = duration.Replace("h", "").Replace("m", "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return depTime;

        if (!int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m))
            return depTime;

        return dt.AddHours(h).AddMinutes(m).ToString("yyyy-MM-dd HH:mm");
    }
}
