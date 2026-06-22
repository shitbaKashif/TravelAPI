namespace TravelBookingAPI.Models;

// ── Search Request ──────────────────────────────────────────────────────────
public class FlightSearchRequest
{
    public string TravelPurpose { get; set; } = string.Empty;
    public string PurposeDetail { get; set; } = string.Empty;
    public string FromCity    { get; set; } = string.Empty;
    public string ToCity      { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public DateTime DepartureDate { get; set; }
    public string DepartureTime { get; set; } = string.Empty; // HH:mm — outbound leg
    /// <summary>Return-leg departure time (same calendar day as DepartureDate); used when IsSameDay is true.</summary>
    public string ReturnTime { get; set; } = string.Empty;
    public DateTime? ReturnDate   { get; set; }
    public bool IsRoundTrip   { get; set; }
    public bool IsSameDay     { get; set; }
    public string CabinClass  { get; set; } = "Economy";   // Economy | Business
    public string PreferredAirline { get; set; } = string.Empty;
    public int Passengers     { get; set; } = 1;
    public bool IncludeHotel  { get; set; }
    public int HotelNights    { get; set; }
}

// ── Flight Result ──────────────────────────────────────────────────────────
public class FlightResult
{
    public string   FlightId       { get; set; } = string.Empty;
    public string   Airline        { get; set; } = string.Empty;
    public string   FromCity       { get; set; } = string.Empty;
    public string   FromCode       { get; set; } = string.Empty;
    public string   ToCity         { get; set; } = string.Empty;
    public string   ToCode         { get; set; } = string.Empty;
    public string   DepartureTime  { get; set; } = string.Empty;
    public string   ArrivalTime    { get; set; } = string.Empty;
    public string   Duration       { get; set; } = string.Empty;
    public string   CabinClass     { get; set; } = string.Empty;
    public string   TripType       { get; set; } = string.Empty;  // One-Way | Round-Trip
    public decimal  PricePerPerson { get; set; }
    public decimal  TotalPrice     { get; set; }
    public string   Currency       { get; set; } = "PKR";
    public string   FlightType     { get; set; } = string.Empty;  // Domestic | International
}

// ── Hotel Result ──────────────────────────────────────────────────────────
public class HotelResult
{
    public string  HotelId       { get; set; } = string.Empty;
    public string  Name          { get; set; } = string.Empty;
    public int     Stars         { get; set; }
    public string  Location      { get; set; } = string.Empty;
    public string  City          { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public string  Currency      { get; set; } = "PKR";
    public string  Amenities     { get; set; } = string.Empty;
}

// ── Booking Submission ─────────────────────────────────────────────────────
public class BookingSubmission
{
    public string  PassengerName    { get; set; } = string.Empty;
    public string  PassengerEmail   { get; set; } = string.Empty;
    public string  PassengerPhone   { get; set; } = string.Empty;
    public string  EmployeeEmail    { get; set; } = string.Empty;  // notified party
    public string  EmployeeName     { get; set; } = string.Empty;
    public string  Department       { get; set; } = string.Empty;
    public FlightSearchRequest SearchCriteria { get; set; } = new();
    public string  SelectedFlightId { get; set; } = string.Empty;
    public string? SelectedHotelId  { get; set; }
}

// ── Booking Response ───────────────────────────────────────────────────────
public class BookingResponse
{
    public string        BookingReference  { get; set; } = string.Empty;
    public FlightResult? SelectedFlight    { get; set; }
    public HotelResult?  SelectedHotel     { get; set; }
    public decimal       FlightTotal       { get; set; }
    public decimal       HotelTotal        { get; set; }
    public decimal       GrandTotal        { get; set; }
    public string        Currency          { get; set; } = "PKR";
    public string        Status            { get; set; } = string.Empty;
    public string        Message           { get; set; } = string.Empty;
    public DateTime      SubmittedAt       { get; set; }
}

// ── Misc ───────────────────────────────────────────────────────────────────
public class SearchResponse
{
    public List<FlightResult> Flights { get; set; } = new();
    public List<HotelResult>  Hotels  { get; set; } = new();
}

public class CityInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Type   { get; set; } = string.Empty;   // Domestic | International
}

public class QuestionField
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public bool Required { get; set; }
    public List<string> Options { get; set; } = new();
}
