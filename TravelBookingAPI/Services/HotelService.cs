using TravelBookingAPI.Models;

namespace TravelBookingAPI.Services;

public interface IHotelService
{
    List<HotelResult> GetHotels(string city, int nights, int passengers);
}

public class HotelService : IHotelService
{
    // Static hotel catalogue — ordered by price so UI can show low → high
    private static readonly List<HotelResult> _hotels = new()
    {
        // ── Karachi ──────────────────────────────────────────────────────
        new() { HotelId="H-KHI-01", Name="Avari Towers",            Stars=5, Location="Fatima Jinnah Road, Karachi",     City="Karachi",     PricePerNight=32000,  Currency="PKR", Amenities="Pool, Gym, Spa, Restaurant, Free WiFi" },
        new() { HotelId="H-KHI-02", Name="Pearl Continental Karachi",Stars=5, Location="Club Road, Karachi",              City="Karachi",     PricePerNight=35000,  Currency="PKR", Amenities="Pool, Gym, Business Center, Restaurant" },
        new() { HotelId="H-KHI-03", Name="Movenpick Hotel",         Stars=5, Location="Club Road, Karachi",              City="Karachi",     PricePerNight=40000,  Currency="PKR", Amenities="Pool, Spa, Multiple Restaurants, Bar" },
        new() { HotelId="H-KHI-04", Name="Marriott Hotel Karachi",  Stars=5, Location="Abdullah Haroon Road, Karachi",   City="Karachi",     PricePerNight=38000,  Currency="PKR", Amenities="Pool, Gym, Spa, Business Center" },
        new() { HotelId="H-KHI-05", Name="Hotel Regent Plaza",      Stars=4, Location="Sharae Faisal, Karachi",          City="Karachi",     PricePerNight=18000,  Currency="PKR", Amenities="Restaurant, Business Center, Free WiFi" },
        new() { HotelId="H-KHI-06", Name="Hotel Crown Inn",         Stars=3, Location="Saddar, Karachi",                 City="Karachi",     PricePerNight=8000,   Currency="PKR", Amenities="Restaurant, Free WiFi" },

        // ── Lahore ───────────────────────────────────────────────────────
        new() { HotelId="H-LHE-01", Name="Pearl Continental Lahore", Stars=5, Location="Shahrah-e-Quaid-e-Azam, Lahore", City="Lahore",      PricePerNight=28000,  Currency="PKR", Amenities="Pool, Gym, Spa, Restaurant, Business Center" },
        new() { HotelId="H-LHE-02", Name="Avari Hotel Lahore",       Stars=5, Location="87-Shahrah-e-Quaid-e-Azam",      City="Lahore",      PricePerNight=25000,  Currency="PKR", Amenities="Pool, Gym, Restaurant, Free WiFi" },
        new() { HotelId="H-LHE-03", Name="Nishat Hotel Lahore",      Stars=5, Location="Gulberg III, Lahore",             City="Lahore",      PricePerNight=22000,  Currency="PKR", Amenities="Pool, Spa, Restaurant" },
        new() { HotelId="H-LHE-04", Name="Faletti's Hotel",          Stars=4, Location="Egerton Road, Lahore",            City="Lahore",      PricePerNight=15000,  Currency="PKR", Amenities="Restaurant, Garden, Free WiFi" },
        new() { HotelId="H-LHE-05", Name="Hotel One Gulberg",        Stars=3, Location="Gulberg II, Lahore",              City="Lahore",      PricePerNight=9000,   Currency="PKR", Amenities="Restaurant, Gym, Free WiFi" },

        // ── Islamabad ────────────────────────────────────────────────────
        new() { HotelId="H-ISB-01", Name="Serena Hotel Islamabad",   Stars=5, Location="Khayaban-e-Suhrawardy, Islamabad",City="Islamabad",   PricePerNight=35000,  Currency="PKR", Amenities="Pool, Gym, Spa, Restaurant, Garden" },
        new() { HotelId="H-ISB-02", Name="Marriott Islamabad",       Stars=5, Location="Aga Khan Road, Islamabad",         City="Islamabad",   PricePerNight=38000,  Currency="PKR", Amenities="Pool, Gym, Spa, Business Center" },
        new() { HotelId="H-ISB-03", Name="Islamabad Ramada",         Stars=4, Location="Convention Road, Islamabad",       City="Islamabad",   PricePerNight=18000,  Currency="PKR", Amenities="Restaurant, Gym, Free WiFi" },
        new() { HotelId="H-ISB-04", Name="Hotel One Blue Area",      Stars=3, Location="Blue Area, Islamabad",             City="Islamabad",   PricePerNight=9500,   Currency="PKR", Amenities="Restaurant, Free WiFi" },

        // ── Peshawar ─────────────────────────────────────────────────────
        new() { HotelId="H-PEW-01", Name="Pearl Continental Peshawar",Stars=5,Location="Khyber Road, Peshawar",            City="Peshawar",    PricePerNight=22000,  Currency="PKR", Amenities="Pool, Restaurant, Business Center" },
        new() { HotelId="H-PEW-02", Name="Shelton's Rezidor",        Stars=4, Location="Saddar Road, Peshawar",            City="Peshawar",    PricePerNight=14000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        // ── Quetta ───────────────────────────────────────────────────────
        new() { HotelId="H-UET-01", Name="Serena Hotel Quetta",      Stars=5, Location="Shahrah-e-Zarghoon, Quetta",      City="Quetta",      PricePerNight=20000,  Currency="PKR", Amenities="Restaurant, Garden, Free WiFi" },
        new() { HotelId="H-UET-02", Name="Hotel Crown Quetta",       Stars=3, Location="Jinnah Road, Quetta",             City="Quetta",      PricePerNight=7500,   Currency="PKR", Amenities="Restaurant, Free WiFi" },

        // ── Multan ───────────────────────────────────────────────────────
        new() { HotelId="H-MUX-01", Name="Ramada Multan",            Stars=4, Location="Abdali Road, Multan",             City="Multan",      PricePerNight=16000,  Currency="PKR", Amenities="Pool, Restaurant, Gym" },
        new() { HotelId="H-MUX-02", Name="Hotel One Multan",         Stars=3, Location="New Shalimar Colony, Multan",     City="Multan",      PricePerNight=8000,   Currency="PKR", Amenities="Restaurant, Free WiFi" },

        // ── International ─────────────────────────────────────────────────
        new() { HotelId="H-DXB-01", Name="Atlantis The Palm",        Stars=5, Location="Palm Jumeirah, Dubai",            City="Dubai",       PricePerNight=120000, Currency="PKR", Amenities="Aquapark, Beach, Multiple Pools, Spa, Gym" },
        new() { HotelId="H-DXB-02", Name="Burj Al Arab Jumeirah",    Stars=5, Location="Jumeirah Road, Dubai",            City="Dubai",       PricePerNight=250000, Currency="PKR", Amenities="Beach, Helicopter Pad, Spa, Multiple Restaurants" },
        new() { HotelId="H-DXB-03", Name="JW Marriott Marquis Dubai",Stars=5, Location="Sheikh Zayed Road, Dubai",        City="Dubai",       PricePerNight=85000,  Currency="PKR", Amenities="Pool, Gym, Spa, Business Center" },
        new() { HotelId="H-DXB-04", Name="Ibis Dubai City Centre",   Stars=3, Location="Deira, Dubai",                   City="Dubai",       PricePerNight=28000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        new() { HotelId="H-LHR-01", Name="The Ritz London",          Stars=5, Location="Piccadilly, London",              City="London",      PricePerNight=180000, Currency="PKR", Amenities="Restaurant, Spa, Butler Service, Bar" },
        new() { HotelId="H-LHR-02", Name="Premier Inn London City",  Stars=3, Location="City of London",                  City="London",      PricePerNight=42000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        new() { HotelId="H-IST-01", Name="Four Seasons Istanbul",    Stars=5, Location="Sultanahmet, Istanbul",           City="Istanbul",    PricePerNight=95000,  Currency="PKR", Amenities="Pool, Spa, Restaurant, Bosphorus View" },
        new() { HotelId="H-IST-02", Name="Ibis Istanbul Zeytinburnu",Stars=3, Location="Zeytinburnu, Istanbul",           City="Istanbul",    PricePerNight=22000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        new() { HotelId="H-DOH-01", Name="W Doha",                   Stars=5, Location="West Bay, Doha",                  City="Doha",        PricePerNight=90000,  Currency="PKR", Amenities="Pool, Beach, Spa, Gym, Multiple Restaurants" },
        new() { HotelId="H-DOH-02", Name="Ibis Doha",                Stars=3, Location="Al Sadd, Doha",                   City="Doha",        PricePerNight=24000,  Currency="PKR", Amenities="Restaurant, Free WiFi, Gym" },

        new() { HotelId="H-BKK-01", Name="Mandarin Oriental Bangkok",Stars=5, Location="Riverside, Bangkok",              City="Bangkok",     PricePerNight=80000,  Currency="PKR", Amenities="Pool, Spa, River View, Multiple Restaurants" },
        new() { HotelId="H-BKK-02", Name="ibis Bangkok Nana",        Stars=3, Location="Nana, Bangkok",                   City="Bangkok",     PricePerNight=18000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        new() { HotelId="H-KUL-01", Name="Mandarin Oriental KL",     Stars=5, Location="KLCC, Kuala Lumpur",              City="Kuala Lumpur",PricePerNight=72000,  Currency="PKR", Amenities="Pool, Gym, Spa, Twin Towers View" },
        new() { HotelId="H-KUL-02", Name="Ibis Kuala Lumpur",        Stars=3, Location="City Centre, Kuala Lumpur",       City="Kuala Lumpur",PricePerNight=16000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        new() { HotelId="H-YYZ-01", Name="Fairmont Royal York",      Stars=5, Location="Front Street, Toronto",           City="Toronto",     PricePerNight=115000, Currency="PKR", Amenities="Pool, Spa, Gym, Multiple Restaurants" },
        new() { HotelId="H-YYZ-02", Name="Comfort Hotel Toronto",    Stars=3, Location="Downtown, Toronto",               City="Toronto",     PricePerNight=38000,  Currency="PKR", Amenities="Breakfast Included, Free WiFi" },

        new() { HotelId="H-JFK-01", Name="The Plaza Hotel New York", Stars=5, Location="Fifth Avenue, New York",          City="New York",    PricePerNight=175000, Currency="PKR", Amenities="Spa, Gym, Fine Dining, Central Park View" },
        new() { HotelId="H-JFK-02", Name="ibis New York Times Square",Stars=3,Location="Midtown, New York",              City="New York",    PricePerNight=52000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        new() { HotelId="H-PEK-01", Name="Park Hyatt Beijing",       Stars=5, Location="Chaoyang, Beijing",               City="Beijing",     PricePerNight=88000,  Currency="PKR", Amenities="Pool, Gym, Spa, Restaurant" },
        new() { HotelId="H-PEK-02", Name="Ibis Beijing Sanlitun",    Stars=3, Location="Sanlitun, Beijing",               City="Beijing",     PricePerNight=19000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        new() { HotelId="H-AUH-01", Name="Emirates Palace Abu Dhabi",Stars=5, Location="Corniche Road, Abu Dhabi",        City="Abu Dhabi",   PricePerNight=140000, Currency="PKR", Amenities="Private Beach, Pool, Spa, Gold ATM" },
        new() { HotelId="H-AUH-02", Name="Ibis Abu Dhabi Gate",      Stars=3, Location="Airport Road, Abu Dhabi",         City="Abu Dhabi",   PricePerNight=24000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },

        new() { HotelId="H-RUH-01", Name="Four Seasons Riyadh",      Stars=5, Location="Kingdom Tower, Riyadh",           City="Riyadh",      PricePerNight=95000,  Currency="PKR", Amenities="Pool, Spa, Gym, Multiple Restaurants" },
        new() { HotelId="H-RUH-02", Name="Ibis Riyadh Olaya",        Stars=3, Location="Olaya, Riyadh",                   City="Riyadh",      PricePerNight=22000,  Currency="PKR", Amenities="Restaurant, Free WiFi" },
    };

    public List<HotelResult> GetHotels(string city, int nights, int passengers)
    {
        var matched = _hotels
            .Where(h => h.City.Contains(city, StringComparison.OrdinalIgnoreCase))
            .OrderBy(h => h.PricePerNight)
            .Select(h => new HotelResult
            {
                HotelId       = h.HotelId,
                Name          = h.Name,
                Stars         = h.Stars,
                Location      = h.Location,
                City          = h.City,
                PricePerNight = h.PricePerNight,
                Currency      = h.Currency,
                Amenities     = h.Amenities
            })
            .ToList();

        return matched;
    }
}
