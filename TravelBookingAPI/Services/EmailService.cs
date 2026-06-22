using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TravelBookingAPI.Models;

namespace TravelBookingAPI.Services;

public interface IEmailService
{
    Task SendBookingNotificationAsync(BookingSubmission submission, BookingResponse bookingResponse);
    Task SendConfirmationToPassengerAsync(BookingSubmission submission, BookingResponse bookingResponse);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── Notify the employee/travel-manager ────────────────────────────────
    public async Task SendBookingNotificationAsync(BookingSubmission sub, BookingResponse res)
    {
        string subject = $"[Travel Request] {sub.PassengerName} — {sub.SearchCriteria.FromCity} → {sub.SearchCriteria.ToCity} | Ref: {res.BookingReference}";
        string body    = BuildEmployeeEmailHtml(sub, res);
        await SendEmailAsync(sub.EmployeeEmail, sub.EmployeeName, subject, body);
    }

    // ── Confirm to the passenger ──────────────────────────────────────────
    public async Task SendConfirmationToPassengerAsync(BookingSubmission sub, BookingResponse res)
    {
        string subject = $"Travel Booking Confirmation — {res.BookingReference}";
        string body    = BuildPassengerEmailHtml(sub, res);
        await SendEmailAsync(sub.PassengerEmail, sub.PassengerName, subject, body);
    }

    // ── Core sender ───────────────────────────────────────────────────────
    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var smtp = _config.GetSection("EmailSettings");
        bool enabled = bool.TryParse(smtp["Enabled"], out var parsedEnabled) ? parsedEnabled : true;
        string host     = smtp["Host"]     ?? "smtp.gmail.com";
        int    port     = int.Parse(smtp["Port"]  ?? "587");
        string user     = smtp["Username"] ?? "";
        string pass     = smtp["Password"] ?? "";
        string fromAddr = smtp["From"]     ?? user;
        string fromName = smtp["FromName"] ?? "Travel Booking System";

        if (!enabled)
        {
            _logger.LogInformation("Email sending is disabled by configuration. Skipping email to {To}.", toEmail);
            _logger.LogInformation("Email SUBJECT: {Subject}", subject);
            _logger.LogInformation("Email BODY (plain):\n{Body}", StripHtml(htmlBody));
            return;
        }

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            _logger.LogWarning("Email credentials not configured. Skipping email to {To}.", toEmail);
            _logger.LogInformation("Email SUBJECT: {Subject}", subject);
            _logger.LogInformation("Email BODY (plain):\n{Body}", StripHtml(htmlBody));
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddr));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("Email sent to {To} — {Subject}", toEmail, subject);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "SMTP authentication failed for {User}. For Gmail, use an App Password and set EmailSettings:Enabled=false in development if needed.", user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {To}", toEmail);
        }
    }

    // ── HTML Templates ─────────────────────────────────────────────────────
    private static string BuildEmployeeEmailHtml(BookingSubmission sub, BookingResponse res)
    {
        var f = res.SelectedFlight!;
        var sc = sub.SearchCriteria;
        string hotelSection = res.SelectedHotel != null
            ? $"<tr><th colspan=\"2\" style=\"background:#f0f4ff;padding:8px\">Hotel Details</th></tr>" +
              $"<tr><td>Hotel</td><td><strong>{res.SelectedHotel.Name}</strong> ({res.SelectedHotel.Stars}★)</td></tr>" +
              $"<tr><td>Location</td><td>{res.SelectedHotel.Location}</td></tr>" +
              $"<tr><td>Nights</td><td>{sc.HotelNights}</td></tr>" +
              $"<tr><td>Hotel Cost</td><td><strong>PKR {res.HotelTotal:N0}</strong></td></tr>"
            : "";

        return
            "<!DOCTYPE html><html><head><style>" +
            "body{font-family:Arial,sans-serif;color:#333;}" +
            "table{border-collapse:collapse;width:100%;max-width:600px;}" +
            "td,th{border:1px solid #ddd;padding:10px;text-align:left;}" +
            "th{background:#003580;color:white;}" +
            ".ref{background:#e8f4fd;padding:15px;border-radius:8px;margin:20px 0;}" +
            ".total{background:#003580;color:white;padding:15px;border-radius:8px;font-size:1.2em;margin-top:20px;}" +
            "</style></head><body>" +
            "<h2>✈ New Travel Booking Request</h2>" +
            $"<div class=\"ref\"><strong>Booking Reference:</strong> {res.BookingReference}<br>" +
            $"<strong>Submitted:</strong> {res.SubmittedAt:dd MMM yyyy HH:mm}</div>" +
            "<table>" +
            "<tr><th colspan=\"2\">Passenger Information</th></tr>" +
            $"<tr><td>Name</td><td><strong>{sub.PassengerName}</strong></td></tr>" +
            $"<tr><td>Email</td><td>{sub.PassengerEmail}</td></tr>" +
            $"<tr><td>Phone</td><td>{sub.PassengerPhone}</td></tr>" +
            $"<tr><td>Department</td><td>{sub.Department}</td></tr>" +
            $"<tr><td>Travel Purpose</td><td>{sc.TravelPurpose}</td></tr>" +
            $"<tr><td>Purpose Detail</td><td>{sc.PurposeDetail}</td></tr>" +
            "<tr><th colspan=\"2\" style=\"background:#f0f4ff;padding:8px\">Flight Details</th></tr>" +
            $"<tr><td>Route</td><td><strong>{f.FromCity} ({f.FromCode}) → {f.ToCity} ({f.ToCode})</strong></td></tr>" +
            $"<tr><td>Trip Type</td><td>{f.TripType}</td></tr>" +
            $"<tr><td>Airline</td><td>{f.Airline}</td></tr>" +
            $"<tr><td>Cabin Class</td><td>{f.CabinClass}</td></tr>" +
            $"<tr><td>Departure</td><td>{f.DepartureTime}</td></tr>" +
            (sc.IsSameDay && !string.IsNullOrWhiteSpace(sc.ReturnTime)
                ? $"<tr><td>Return leg (departure time)</td><td>{sc.ReturnTime}</td></tr>"
                : "") +
            $"<tr><td>Arrival</td><td>{f.ArrivalTime}</td></tr>" +
            $"<tr><td>Duration</td><td>{f.Duration}</td></tr>" +
            $"<tr><td>Passengers</td><td>{sc.Passengers}</td></tr>" +
            $"<tr><td>Preferred Air</td><td>{(string.IsNullOrWhiteSpace(sc.PreferredAirline) ? "Any" : sc.PreferredAirline)}</td></tr>" +
            $"<tr><td>Price/Person</td><td>PKR {f.PricePerPerson:N0}</td></tr>" +
            $"<tr><td>Flight Total</td><td><strong>PKR {res.FlightTotal:N0}</strong></td></tr>" +
            hotelSection +
            "</table>" +
            $"<div class=\"total\">Grand Total: PKR {res.GrandTotal:N0}</div>" +
            "<p style=\"color:#888;font-size:0.85em;margin-top:20px\">This is an automated notification from the Travel Booking System.</p>" +
            "</body></html>";
    }

    private static string BuildPassengerEmailHtml(BookingSubmission sub, BookingResponse res)
    {
        var f = res.SelectedFlight!;
        string hotelLine = res.SelectedHotel != null
            ? $"<p>🏨 <strong>Hotel:</strong> {res.SelectedHotel.Name} ({res.SelectedHotel.Stars}★) — {res.SelectedHotel.Location} — PKR {res.HotelTotal:N0} ({sub.SearchCriteria.HotelNights} nights)</p>"
            : "";

        return
            "<!DOCTYPE html><html><head><style>" +
            "body{font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto;}" +
            ".header{background:#003580;color:white;padding:20px;border-radius:8px 8px 0 0;text-align:center;}" +
            ".body{background:#f9f9f9;padding:20px;border:1px solid #ddd;}" +
            ".ref-box{background:#e8f4fd;padding:15px;border-radius:6px;margin:15px 0;font-size:1.2em;}" +
            ".flight-box{background:white;padding:15px;border-radius:6px;border-left:4px solid #003580;margin:10px 0;}" +
            ".total-box{background:#003580;color:white;padding:15px;border-radius:6px;text-align:center;font-size:1.1em;}" +
            "</style></head><body>" +
            "<div class=\"header\"><h2>✈ Booking Confirmation</h2><p>Thank you for your travel request!</p></div>" +
            "<div class=\"body\">" +
            $"<div class=\"ref-box\">📋 <strong>Booking Reference: {res.BookingReference}</strong></div>" +
            $"<p>Dear <strong>{sub.PassengerName}</strong>,</p>" +
            "<p>Your travel request has been submitted and forwarded to the travel desk. Below are your booking details:</p>" +
            "<div class=\"flight-box\">" +
            $"<strong>✈ {f.Airline}</strong> — {f.CabinClass} | {f.TripType}<br>" +
            $"<strong>{f.FromCity} ({f.FromCode}) → {f.ToCity} ({f.ToCode})</strong><br>" +
            $"🕐 Departure: {f.DepartureTime} | Arrival: {f.ArrivalTime}<br>" +
            $"🧾 Purpose: {sub.SearchCriteria.TravelPurpose} — {sub.SearchCriteria.PurposeDetail}<br>" +
            $"⏱ Duration: {f.Duration} | 👥 Passengers: {sub.SearchCriteria.Passengers}<br>" +
            $"💰 Flight Total: <strong>PKR {res.FlightTotal:N0}</strong>" +
            "</div>" +
            hotelLine +
            $"<div class=\"total-box\">Grand Total: PKR {res.GrandTotal:N0}</div>" +
            $"<p style=\"margin-top:20px\">Your request has been forwarded to <strong>{(string.IsNullOrWhiteSpace(sub.EmployeeName) ? "Travel Desk" : sub.EmployeeName)}</strong> for processing.</p>" +
            $"<p style=\"color:#888;font-size:0.85em\">Submitted: {res.SubmittedAt:dd MMM yyyy HH:mm}</p>" +
            "</div></body></html>";
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "").Trim();
}
