using System.Text.Json.Serialization;

namespace TravelBookingAPI.Models;

// ── Auth ──────────────────────────────────────────────────────────────────────
public class BookmeAuthRequest
{
    [JsonPropertyName("username")] public string Username { get; set; } = "";
    [JsonPropertyName("password")] public string Password { get; set; } = "";
}

public class BookmeAuthResponse
{
    [JsonPropertyName("Token")] public string Token { get; set; } = "";
}

// ── Search ────────────────────────────────────────────────────────────────────
public class BookmeSearchRequest
{
    [JsonPropertyName("Currency")]       public string Currency      { get; set; } = "PKR";
    [JsonPropertyName("TripType")]       public string TripType      { get; set; } = "one_way"; // one_way | return
    [JsonPropertyName("Locations")]      public List<BookmeLocation>  Locations      { get; set; } = new();
    [JsonPropertyName("TravelingDates")] public List<string>          TravelingDates { get; set; } = new();
    [JsonPropertyName("Travelers")]      public List<BookmeTraveler>  Travelers      { get; set; } = new();
    [JsonPropertyName("TravelClass")]    public string TravelClass   { get; set; } = "economy";  // economy | business
    [JsonPropertyName("FlexibleDates")]  public bool   FlexibleDates { get; set; } = false;
    [JsonPropertyName("ContentProvider")] public string ContentProvider { get; set; } = "";
}

public class BookmeLocation
{
    [JsonPropertyName("IATA")] public string IATA { get; set; } = "";
    [JsonPropertyName("Type")] public string Type { get; set; } = "airport";
}

public class BookmeTraveler
{
    [JsonPropertyName("Type")]  public string Type  { get; set; } = "adult";
    [JsonPropertyName("Count")] public int    Count { get; set; } = 1;
}

// ── Search Response ───────────────────────────────────────────────────────────
public class BookmeSearchResponse
{
    [JsonPropertyName("RefID")]       public string RefID { get; set; } = "";
    [JsonPropertyName("Itineraries")] public List<BookmeItinerary>? Itineraries { get; set; }
    [JsonPropertyName("Data")]        public List<BookmeItinerary>? Data        { get; set; }
    [JsonPropertyName("Results")]     public List<BookmeItinerary>? Results     { get; set; }

    public List<BookmeItinerary> GetItineraries() => Itineraries ?? Data ?? Results ?? new();
}

public class BookmeItinerary
{
    [JsonPropertyName("RefID")]       public string RefID { get; set; } = "";
    [JsonPropertyName("TotalPrice")]  public decimal? TotalPrice { get; set; }
    [JsonPropertyName("Total")]       public decimal? Total      { get; set; }
    [JsonPropertyName("BasePrice")]   public decimal? BasePrice  { get; set; }
    [JsonPropertyName("Tax")]         public decimal? Tax        { get; set; }
    [JsonPropertyName("Currency")]    public string?  Currency   { get; set; }
    [JsonPropertyName("Passengers")]  public List<BookmePassengerRef>? Passengers { get; set; }
    [JsonPropertyName("Flights")]     public List<BookmeFlight>?       Flights    { get; set; }

    public decimal GetTotal() => TotalPrice ?? Total ?? (BasePrice ?? 0m) + (Tax ?? 0m);
}

public class BookmeFlight
{
    [JsonPropertyName("Sequence")] public int Sequence { get; set; } = 1;
    [JsonPropertyName("Fares")]    public List<BookmeFare>?    Fares    { get; set; }
    [JsonPropertyName("Segments")] public List<BookmeSegment>? Segments { get; set; }
}

public class BookmeFare
{
    [JsonPropertyName("RefID")]        public string  RefID       { get; set; } = "";
    [JsonPropertyName("TravelClass")]  public string? TravelClass { get; set; }
    [JsonPropertyName("Class")]        public string? Class       { get; set; }
    [JsonPropertyName("TotalPrice")]   public decimal? TotalPrice { get; set; }
    [JsonPropertyName("Total")]        public decimal? Total      { get; set; }
    [JsonPropertyName("PerPax")]       public decimal? PerPax     { get; set; }
    [JsonPropertyName("BasePrice")]    public decimal? BasePrice  { get; set; }

    public string  GetClass()  => TravelClass ?? Class ?? "economy";
    public decimal GetTotal()  => TotalPrice ?? Total ?? BasePrice ?? 0m;
    public decimal GetPerPax() => PerPax ?? GetTotal();
}

public class BookmeSegment
{
    [JsonPropertyName("Sequence")]         public int Sequence { get; set; } = 1;
    [JsonPropertyName("MarketingCarrier")] public BookmeCarrier?    MarketingCarrier { get; set; }
    [JsonPropertyName("OperatingCarrier")] public BookmeCarrier?    OperatingCarrier { get; set; }
    [JsonPropertyName("Departure")]        public BookmeAirportTime? Departure       { get; set; }
    [JsonPropertyName("Arrival")]          public BookmeAirportTime? Arrival         { get; set; }
    [JsonPropertyName("Duration")]         public string? Duration { get; set; }
    [JsonPropertyName("FlightNumber")]     public string? FlightNumber { get; set; }
}

public class BookmeCarrier
{
    [JsonPropertyName("Code")]         public string  Code         { get; set; } = "";
    [JsonPropertyName("Name")]         public string? Name         { get; set; }
    [JsonPropertyName("FlightNumber")] public string? FlightNumber { get; set; }
}

public class BookmeAirportTime
{
    [JsonPropertyName("IATA")]     public string  IATA     { get; set; } = "";
    [JsonPropertyName("Time")]     public string? Time     { get; set; }
    [JsonPropertyName("DateTime")] public string? DateTime { get; set; }
    [JsonPropertyName("Terminal")] public string? Terminal { get; set; }
    [JsonPropertyName("Name")]     public string? Name     { get; set; }

    public string GetTime() => Time ?? DateTime ?? "";
}

public class BookmePassengerRef
{
    [JsonPropertyName("RefID")] public string RefID { get; set; } = "";
    [JsonPropertyName("Type")]  public string Type  { get; set; } = "adult";
}

// ── Quote ─────────────────────────────────────────────────────────────────────
public class BookmeQuoteRequest
{
    [JsonPropertyName("RefID")]          public string RefID          { get; set; } = "";
    [JsonPropertyName("ItineraryRefID")] public string ItineraryRefID { get; set; } = "";
    [JsonPropertyName("Flights")]        public List<BookmeQuoteFlight> Flights { get; set; } = new();
}

public class BookmeQuoteFlight
{
    [JsonPropertyName("Sequence")]   public int    Sequence   { get; set; } = 1;
    [JsonPropertyName("FlightFare")] public string FlightFare { get; set; } = "";
}

public class BookmeQuoteResponse
{
    [JsonPropertyName("RefID")]      public string  RefID    { get; set; } = "";
    [JsonPropertyName("Passengers")] public List<BookmePassengerRef>? Passengers { get; set; }
    [JsonPropertyName("TotalPrice")] public decimal? TotalPrice { get; set; }
    [JsonPropertyName("Total")]      public decimal? Total      { get; set; }
    [JsonPropertyName("Currency")]   public string   Currency   { get; set; } = "PKR";
    [JsonPropertyName("Status")]     public string?  Status     { get; set; }

    public decimal                  GetTotal()      => TotalPrice ?? Total ?? 0m;
    public List<BookmePassengerRef> GetPassengers() => Passengers ?? new();
}

// ── Reserve ───────────────────────────────────────────────────────────────────
public class BookmeReserveRequest
{
    [JsonPropertyName("RefID")]          public string RefID { get; set; } = "";
    [JsonPropertyName("Passengers")]     public List<BookmeReservePassenger> Passengers     { get; set; } = new();
    [JsonPropertyName("ContactDetails")] public BookmeContactDetails         ContactDetails { get; set; } = new();
}

public class BookmeReservePassenger
{
    [JsonPropertyName("RefID")]                  public string  RefID                  { get; set; } = "";
    [JsonPropertyName("Title")]                  public string  Title                  { get; set; } = "mr";
    [JsonPropertyName("FirstName")]              public string  FirstName              { get; set; } = "";
    [JsonPropertyName("MiddleName")]             public string  MiddleName             { get; set; } = "";
    [JsonPropertyName("LastName")]               public string  LastName               { get; set; } = "";
    [JsonPropertyName("Gender")]                 public string  Gender                 { get; set; } = "male";
    [JsonPropertyName("DOB")]                    public string  DOB                    { get; set; } = "1990-01-01";
    [JsonPropertyName("Nationality")]            public string  Nationality            { get; set; } = "PK";
    [JsonPropertyName("NationalID")]             public string  NationalID             { get; set; } = "";
    [JsonPropertyName("PassportExpiry")]         public string? PassportExpiry         { get; set; }
    [JsonPropertyName("PassportIssuingCountry")] public string? PassportIssuingCountry { get; set; }
    [JsonPropertyName("PassportIssunance")]      public string? PassportIssunance      { get; set; }
    [JsonPropertyName("PassportNumber")]         public string? PassportNumber         { get; set; }
    [JsonPropertyName("FrequentFlyerNo")]        public string? FrequentFlyerNo        { get; set; }
}

public class BookmeContactDetails
{
    [JsonPropertyName("EmailAddress")]       public string EmailAddress       { get; set; } = "";
    [JsonPropertyName("Title")]              public string Title              { get; set; } = "mr";
    [JsonPropertyName("FirstName")]          public string FirstName          { get; set; } = "";
    [JsonPropertyName("MiddleName")]         public string MiddleName         { get; set; } = "";
    [JsonPropertyName("LastName")]           public string LastName           { get; set; } = "";
    [JsonPropertyName("Nationality")]        public string Nationality        { get; set; } = "PK";
    [JsonPropertyName("PhoneNumber")]        public string PhoneNumber        { get; set; } = "";
    [JsonPropertyName("PhoneNumberCountry")] public string PhoneNumberCountry { get; set; } = "PK";
}

public class BookmeReserveResponse
{
    [JsonPropertyName("OrderRefId")] public string? OrderRefId { get; set; }
    [JsonPropertyName("BookingRef")] public string? BookingRef { get; set; }
    [JsonPropertyName("PNR")]        public string? PNR        { get; set; }
    [JsonPropertyName("Status")]     public string? Status     { get; set; }
    [JsonPropertyName("Message")]    public string? Message    { get; set; }

    public string GetOrderRefId() => OrderRefId ?? BookingRef ?? PNR ?? "";
}

// ── IPN (e-ticket issuance) ───────────────────────────────────────────────────
public class BookmeIpnRequest
{
    [JsonPropertyName("api_key")]    public string  ApiKey    { get; set; } = "";
    [JsonPropertyName("OrderRefId")] public string  OrderRefId { get; set; } = "";
    [JsonPropertyName("Type")]       public string  Type      { get; set; } = "airline";
    [JsonPropertyName("Status")]     public string  Status    { get; set; } = "confirm";
    [JsonPropertyName("Amount")]     public decimal Amount    { get; set; }
    [JsonPropertyName("PartnerId")]  public string  PartnerId { get; set; } = "";
}

// ── Cache payload stored per flight result ────────────────────────────────────
public class BookmeFlightSession
{
    public string SearchRefID    { get; set; } = "";
    public string ItineraryRefID { get; set; } = "";
    public List<BookmeQuoteFlight> QuoteFlights { get; set; } = new();
}
