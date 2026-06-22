using Microsoft.AspNetCore.Mvc;
using TravelBookingAPI.Models;
using TravelBookingAPI.Services;

namespace TravelBookingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TravelController : ControllerBase
{
    private readonly IFlightDataService _flightService;
    private readonly IHotelService      _hotelService;
    private readonly IEmailService      _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<TravelController> _logger;

    public TravelController(
        IFlightDataService flightService,
        IHotelService hotelService,
        IEmailService emailService,
        IConfiguration config,
        ILogger<TravelController> logger)
    {
        _flightService = flightService;
        _hotelService  = hotelService;
        _emailService  = emailService;
        _config        = config;
        _logger        = logger;
    }

    // ── GET /api/travel/cities ─────────────────────────────────────────────
    /// <summary>Returns all available departure/arrival cities from the data file.</summary>
    [HttpGet("cities")]
    public async Task<ActionResult<List<CityInfo>>> GetCities(CancellationToken cancellationToken)
    {
        var cities = await _flightService.GetAllCitiesAsync(cancellationToken);
        return Ok(cities);
    }

    // ── GET /api/travel/questions ──────────────────────────────────────────
    /// <summary>Returns the booking questions based on travel form requirements.</summary>
    [HttpGet("questions")]
    public ActionResult<List<QuestionField>> GetQuestions()
    {
        var questions = new List<QuestionField>
        {
            new() { Key = "travelPurpose", Label = "Select Travel Purpose", InputType = "select", Required = true },
            new() { Key = "purposeDetail", Label = "Purpose in Detail", InputType = "text", Required = true },
            new() { Key = "tripType", Label = "Trip Type", InputType = "select", Required = true, Options = new() { "One-way", "Round Trip", "Same Day" } },
            new() { Key = "fromCity", Label = "Select City From", InputType = "text", Required = true },
            new() { Key = "toCity", Label = "Select City To", InputType = "text", Required = true },
            new() { Key = "distanceKm", Label = "Distance (in KMs)", InputType = "number", Required = false },
            new() { Key = "departureDate", Label = "Departure Date", InputType = "date", Required = true },
            new() { Key = "departureTime", Label = "Departure Time", InputType = "time", Required = true },
            new() { Key = "preferredAirline", Label = "Air", InputType = "text", Required = false }
        };

        return Ok(questions);
    }

    // ── POST /api/travel/search ────────────────────────────────────────────
    /// <summary>
    /// Searches for available flights (and optionally hotels) based on the
    /// user's travel criteria.  Returns flights sorted lowest → highest price.
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<SearchResponse>> Search([FromBody] FlightSearchRequest req, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(req.TravelPurpose))
            return BadRequest("TravelPurpose is required.");
        if (string.IsNullOrWhiteSpace(req.PurposeDetail))
            return BadRequest("PurposeDetail is required.");
        if (string.IsNullOrWhiteSpace(req.FromCity) || string.IsNullOrWhiteSpace(req.ToCity))
            return BadRequest("FromCity and ToCity are required.");

        if (req.DepartureDate < DateTime.Today)
            return BadRequest("Departure date cannot be in the past.");

        if (req.Passengers < 1 || req.Passengers > 9)
            return BadRequest("Passengers must be between 1 and 9.");
        if (string.IsNullOrWhiteSpace(req.DepartureTime))
            return BadRequest("DepartureTime is required.");

        if (req.IsSameDay)
        {
            req.IsRoundTrip = true;
            req.ReturnDate = req.DepartureDate;
            if (string.IsNullOrWhiteSpace(req.ReturnTime))
                return BadRequest("ReturnTime is required for same-day trips.");
        }
        else if (req.IsRoundTrip)
        {
            if (req.ReturnDate == null || req.ReturnDate <= req.DepartureDate)
                return BadRequest("Return date must be after departure date for round trips.");
        }

        var flights = await _flightService.SearchFlightsAsync(req, cancellationToken);

        var hotels = new List<HotelResult>();
        if (req.IncludeHotel && req.HotelNights > 0)
            hotels = _hotelService.GetHotels(req.ToCity, req.HotelNights, req.Passengers);

        return Ok(new SearchResponse { Flights = flights, Hotels = hotels });
    }

    // ── POST /api/travel/book ─────────────────────────────────────────────
    /// <summary>
    /// Submits the final booking, builds a booking reference, calculates totals,
    /// and sends email notifications to both the passenger and the employee.
    /// </summary>
    [HttpPost("book")]
    public async Task<ActionResult<BookingResponse>> Book([FromBody] BookingSubmission sub)
    {
        // ── Basic validation ──────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(sub.PassengerName))
            return BadRequest("PassengerName is required.");
        if (string.IsNullOrWhiteSpace(sub.PassengerEmail) || !sub.PassengerEmail.Contains('@'))
            return BadRequest("A valid PassengerEmail is required.");
        if (string.IsNullOrWhiteSpace(sub.SelectedFlightId))
            return BadRequest("SelectedFlightId is required.");
        if (string.IsNullOrWhiteSpace(sub.SearchCriteria.TravelPurpose))
            return BadRequest("TravelPurpose is required.");
        if (string.IsNullOrWhiteSpace(sub.SearchCriteria.PurposeDetail))
            return BadRequest("PurposeDetail is required.");

        // Default notification recipient to Travel Desk if frontend does not provide employee fields.
        if (string.IsNullOrWhiteSpace(sub.EmployeeEmail) || !sub.EmployeeEmail.Contains('@'))
            sub.EmployeeEmail = _config["TravelDesk:Email"] ?? _config["EmailSettings:From"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sub.EmployeeName))
            sub.EmployeeName = _config["TravelDesk:Name"] ?? "Travel Desk";

        if (string.IsNullOrWhiteSpace(sub.EmployeeEmail))
            return BadRequest("Travel desk email is not configured. Set TravelDesk:Email in appsettings.");

        if (sub.SearchCriteria.IsSameDay)
        {
            sub.SearchCriteria.IsRoundTrip = true;
            sub.SearchCriteria.ReturnDate = sub.SearchCriteria.DepartureDate;
        }

        // ── Re-run the search to resolve selected flight ──────────────────
        var flights = await _flightService.SearchFlightsAsync(sub.SearchCriteria);
        var selectedFlight = flights.FirstOrDefault(f => f.FlightId == sub.SelectedFlightId);

        if (selectedFlight == null)
            return BadRequest($"Flight '{sub.SelectedFlightId}' not found. Please search again.");

        // ── Resolve optional hotel ────────────────────────────────────────
        HotelResult? selectedHotel = null;
        decimal hotelTotal = 0;
        if (!string.IsNullOrWhiteSpace(sub.SelectedHotelId) && sub.SearchCriteria.HotelNights > 0)
        {
            var hotels = _hotelService.GetHotels(sub.SearchCriteria.ToCity,
                                                  sub.SearchCriteria.HotelNights,
                                                  sub.SearchCriteria.Passengers);
            selectedHotel = hotels.FirstOrDefault(h => h.HotelId == sub.SelectedHotelId);
            if (selectedHotel != null)
                hotelTotal = selectedHotel.PricePerNight * sub.SearchCriteria.HotelNights;
        }

        // ── Build response ─────────────────────────────────────────────────
        var response = new BookingResponse
        {
            BookingReference = GenerateRef(),
            SelectedFlight   = selectedFlight,
            SelectedHotel    = selectedHotel,
            FlightTotal      = selectedFlight.TotalPrice,
            HotelTotal       = hotelTotal,
            GrandTotal       = selectedFlight.TotalPrice + hotelTotal,
            Currency         = "PKR",
            Status           = "Submitted",
            Message          = "Your booking has been submitted and the travel team has been notified.",
            SubmittedAt      = DateTime.Now
        };

        // ── Send emails (fire-and-forget with error logging) ───────────────
        _ = Task.WhenAll(
            SafeSendAsync(() => _emailService.SendBookingNotificationAsync(sub, response)),
            SafeSendAsync(() => _emailService.SendConfirmationToPassengerAsync(sub, response))
        );

        _logger.LogInformation("Booking {Ref} created for {Name} — {From}→{To}",
            response.BookingReference, sub.PassengerName,
            sub.SearchCriteria.FromCity, sub.SearchCriteria.ToCity);

        return Ok(response);
    }

    // ── GET /api/travel/health ─────────────────────────────────────────────
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", time = DateTime.Now });

    // ── Helpers ────────────────────────────────────────────────────────────
    private static string GenerateRef()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng   = new Random();
        return "TRV-" + new string(Enumerable.Range(0, 8).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }

    private async Task SafeSendAsync(Func<Task> action)
    {
        try   { await action(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Email send failed (non-fatal)."); }
    }
}
