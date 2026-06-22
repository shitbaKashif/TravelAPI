# Travel Booking API + Frontend

This project provides:

- A .NET API for travel intake, flight search, booking submission, and email notifications.
- A frontend chat UI that guides users through travel form questions step by step.
- Flight pricing sorted from low to high.
- Dual source for flights:
  - **ScrapingBee live search** when `ScrapingBee:ApiKey` is configured.
  - **Excel fallback** (`TravelBookingAPI/Data/FlightData.xlsx`) in PKR when the key is not configured.

## What Was Implemented

- Added travel intake fields:
  - Travel purpose and purpose detail
  - Trip type (`One-Way`, `Round-Trip`, `Same Day`)
  - City from/to
  - Distance KM (optional)
  - Departure date and departure time
  - Return time (same-day trips only)
  - Cabin class (Economy / Business)
  - Preferred airline (optional)
  - Passengers count
  - Hotel inclusion with nights
- Added endpoint to return booking questions:
  - `GET /api/travel/questions`
- Booking flow: select flight → optional hotel → summary → confirm → submit.
- Sends notification to travel desk email and confirmation to passenger email.
- Flight data logic:
  - ScrapingBee web search overlaid on Excel data (prices merged, sorted low→high).
  - Excel-only fallback when ScrapingBee key is absent.

## Project Structure

- Backend: `TravelBookingAPI/`
- Frontend: `frontend/index.html`
- Excel data seeder: `TravelBookingAPI/Data/FlightDataSeeder.cs`
- Flight + hotel services: `TravelBookingAPI/Services/`
- Models: `TravelBookingAPI/Models/TravelModels.cs`

## Run Instructions

1. Open terminal in `TravelBookingAPI`.
2. Build:
   - `dotnet build`
3. Run:
   - `dotnet run`
4. Open:
   - API Swagger: `http://localhost:5099/swagger`
   - Frontend: `http://localhost:5099/`

## Configuration

File: `TravelBookingAPI/appsettings.json`

### Email

Configure:

- `EmailSettings:Host`
- `EmailSettings:Port`
- `EmailSettings:Username`
- `EmailSettings:Password`
- `EmailSettings:From`
- `EmailSettings:FromName`

### Travel Desk (notification recipient)

Configure the employee/desk that receives booking notifications:

- `TravelDesk:Name`
- `TravelDesk:Email`
- `TravelDesk:Number`
- `TravelDesk:Department`

If `TravelDesk:Email` is not set, the API falls back to `EmailSettings:From`.

### ScrapingBee (optional but recommended)

Configure to enable live flight price search:

- `ScrapingBee:BaseUrl` = `https://app.scrapingbee.com`
- `ScrapingBee:ApiKey`
- `ScrapingBee:CountryCode` = `pk`

If the API key is missing, the API automatically falls back to Excel pricing data in PKR.

## API Endpoints

- `GET /api/travel/health`
- `GET /api/travel/questions`
- `GET /api/travel/cities`
- `POST /api/travel/search`
- `POST /api/travel/book`

## Example Search Payload

```json
{
  "travelPurpose": "Official Work",
  "purposeDetail": "Client onboarding meeting",
  "fromCity": "Karachi",
  "toCity": "Lahore",
  "distanceKm": 1020,
  "departureDate": "2026-05-10T00:00:00",
  "departureTime": "09:30",
  "returnDate": null,
  "returnTime": null,
  "isRoundTrip": false,
  "isSameDay": false,
  "cabinClass": "Economy",
  "preferredAirline": "PIA",
  "passengers": 1,
  "includeHotel": false,
  "hotelNights": 0
}
```

## About ScrapingBee API Keys

- Sign up at [https://app.scrapingbee.com](https://app.scrapingbee.com) and copy your API key.
- Free tier is available for testing.
- If you prefer not to use ScrapingBee, leave `ScrapingBee:ApiKey` empty — the API will serve Excel data only.

## Notes

- The frontend (`frontend/index.html`) has booking defaults (passenger name, email, department) hardcoded in `state.booking`. Update these for your environment or wire them to a login flow.
- `BookmeService.cs` / `BookmeModels.cs` are present in the codebase but not registered or used — they are scaffolding for a future Bookme.pk integration.
