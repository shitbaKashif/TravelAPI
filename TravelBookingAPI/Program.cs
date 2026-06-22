using TravelBookingAPI.Data;
using TravelBookingAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Travel Booking API", Version = "v1" });
});

builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddHttpClient<IFlightDataService, FlightDataService>();
builder.Services.AddScoped<IHotelService,      HotelService>();
builder.Services.AddScoped<IEmailService,      EmailService>();

string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
builder.Configuration["FlightDataPath"] = Path.Combine(dataDir, "FlightData.xlsx");

var app = builder.Build();

FlightDataSeeder.EnsureCreated(dataDir);

// ── Middleware ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Travel Booking API v1"));
}

app.UseCors("AllowAll");
if (Directory.Exists(app.Environment.WebRootPath))
{
    app.UseStaticFiles();
}
app.UseAuthorization();
app.MapControllers();

// Serve frontend at root
app.MapGet("/", async ctx =>
{
    var html = Path.Combine(app.Environment.ContentRootPath, "..", "frontend", "index.html");
    if (File.Exists(html))
    {
        ctx.Response.ContentType = "text/html";
        await ctx.Response.SendFileAsync(html);
    }
    else
    {
        ctx.Response.Redirect("/swagger");
    }
});

app.Run();
