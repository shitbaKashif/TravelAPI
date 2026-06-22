using ClosedXML.Excel;

namespace TravelBookingAPI.Data;

/// <summary>
/// Creates FlightData.xlsx on first run with realistic Pakistan domestic
/// and international flight data.
/// </summary>
public static class FlightDataSeeder
{
    public static string ExcelPath { get; private set; } = string.Empty;

    public static void EnsureCreated(string dataDirectory)
    {
        ExcelPath = Path.Combine(dataDirectory, "FlightData.xlsx");

        if (File.Exists(ExcelPath)) return;

        Directory.CreateDirectory(dataDirectory);
        using var wb = new XLWorkbook();

        CreateDomesticSheet(wb);
        CreateInternationalSheet(wb);

        wb.SaveAs(ExcelPath);
    }

    // ── Domestic Sheet ─────────────────────────────────────────────────────
    private static void CreateDomesticSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Domestic");

        // Headers
        string[] headers =
        {
            "FromCity","FromCode","ToCity","ToCode",
            "Airline","Duration",
            "Economy_OneWay","Economy_RoundTrip",
            "Business_OneWay","Business_RoundTrip",
            "Currency"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Data rows  (PKR prices)
        var rows = new object[,]
        {
            // KHI ↔ LHE
            {"Karachi","KHI","Lahore","LHE","PIA","1h 50m",18500,37000,38000,76000,"PKR"},
            {"Karachi","KHI","Lahore","LHE","AirBlue","1h 50m",16500,33000,0,0,"PKR"},
            {"Karachi","KHI","Lahore","LHE","AirSial","1h 55m",15000,30000,0,0,"PKR"},
            {"Karachi","KHI","Lahore","LHE","SereneAir","1h 50m",17000,34000,0,0,"PKR"},

            // KHI ↔ ISB
            {"Karachi","KHI","Islamabad","ISB","PIA","2h 05m",22000,44000,44000,88000,"PKR"},
            {"Karachi","KHI","Islamabad","ISB","AirBlue","2h 05m",20000,40000,0,0,"PKR"},
            {"Karachi","KHI","Islamabad","ISB","AirSial","2h 10m",18500,37000,0,0,"PKR"},
            {"Karachi","KHI","Islamabad","ISB","SereneAir","2h 05m",21000,42000,0,0,"PKR"},

            // LHE ↔ ISB
            {"Lahore","LHE","Islamabad","ISB","PIA","0h 55m",12000,24000,25000,50000,"PKR"},
            {"Lahore","LHE","Islamabad","ISB","AirBlue","0h 55m",11000,22000,0,0,"PKR"},
            {"Lahore","LHE","Islamabad","ISB","AirSial","1h 00m",10000,20000,0,0,"PKR"},

            // KHI ↔ PEW
            {"Karachi","KHI","Peshawar","PEW","PIA","2h 20m",24000,48000,48000,96000,"PKR"},
            {"Karachi","KHI","Peshawar","PEW","AirBlue","2h 20m",19000,38000,0,0,"PKR"},
            {"Karachi","KHI","Peshawar","PEW","AirSial","2h 25m",18000,36000,0,0,"PKR"},

            // KHI ↔ UET (Quetta)
            {"Karachi","KHI","Quetta","UET","PIA","1h 30m",16000,32000,32000,64000,"PKR"},
            {"Karachi","KHI","Quetta","UET","SereneAir","1h 35m",14500,29000,0,0,"PKR"},

            // LHE ↔ PEW
            {"Lahore","LHE","Peshawar","PEW","PIA","1h 10m",15000,30000,30000,60000,"PKR"},
            {"Lahore","LHE","Peshawar","PEW","AirBlue","1h 10m",13000,26000,0,0,"PKR"},

            // ISB ↔ PEW
            {"Islamabad","ISB","Peshawar","PEW","PIA","0h 45m",10000,20000,20000,40000,"PKR"},

            // ISB ↔ UET
            {"Islamabad","ISB","Quetta","UET","PIA","1h 45m",17000,34000,34000,68000,"PKR"},
            {"Islamabad","ISB","Quetta","UET","SereneAir","1h 50m",15500,31000,0,0,"PKR"},

            // KHI ↔ MUX (Multan)
            {"Karachi","KHI","Multan","MUX","PIA","1h 45m",17500,35000,35000,70000,"PKR"},
            {"Karachi","KHI","Multan","MUX","AirBlue","1h 45m",16000,32000,0,0,"PKR"},

            // LHE ↔ MUX
            {"Lahore","LHE","Multan","MUX","PIA","0h 50m",11500,23000,23000,46000,"PKR"},
            {"Lahore","LHE","Multan","MUX","AirBlue","0h 55m",10500,21000,0,0,"PKR"},

            // KHI ↔ LYP (Faisalabad)
            {"Karachi","KHI","Faisalabad","LYP","PIA","1h 55m",19000,38000,38000,76000,"PKR"},
            {"Karachi","KHI","Faisalabad","LYP","AirBlue","2h 00m",17000,34000,0,0,"PKR"},

            // ISB ↔ LYP
            {"Islamabad","ISB","Faisalabad","LYP","PIA","0h 50m",12000,24000,24000,48000,"PKR"},
        };

        for (int r = 0; r < rows.GetLength(0); r++)
        {
            for (int c = 0; c < rows.GetLength(1); c++)
                ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r, c]);
        }

        ws.Columns().AdjustToContents();
    }

    // ── International Sheet ────────────────────────────────────────────────
    private static void CreateInternationalSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("International");

        string[] headers =
        {
            "FromCity","FromCode","ToCity","ToCode",
            "Airline","Duration",
            "Economy_OneWay","Economy_RoundTrip",
            "Business_OneWay","Business_RoundTrip",
            "Currency"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        }

        // PKR prices (1 USD ≈ 280 PKR baseline — adjust if needed)
        var rows = new object[,]
        {
            // ─── Pakistan → Dubai ───────────────────────────────────────
            {"Karachi","KHI","Dubai","DXB","Emirates","2h 20m",52000,104000,210000,420000,"PKR"},
            {"Karachi","KHI","Dubai","DXB","PIA","2h 20m",48000,96000,175000,350000,"PKR"},
            {"Karachi","KHI","Dubai","DXB","FlyDubai","2h 20m",44000,88000,0,0,"PKR"},
            {"Karachi","KHI","Dubai","DXB","AirArabia","2h 25m",42000,84000,0,0,"PKR"},
            {"Islamabad","ISB","Dubai","DXB","Emirates","3h 40m",58000,116000,220000,440000,"PKR"},
            {"Islamabad","ISB","Dubai","DXB","PIA","3h 40m",52000,104000,185000,370000,"PKR"},
            {"Islamabad","ISB","Dubai","DXB","FlyDubai","3h 45m",50000,100000,0,0,"PKR"},
            {"Lahore","LHE","Dubai","DXB","Emirates","3h 10m",56000,112000,215000,430000,"PKR"},
            {"Lahore","LHE","Dubai","DXB","PIA","3h 15m",50000,100000,180000,360000,"PKR"},

            // ─── Pakistan → Abu Dhabi ────────────────────────────────────
            {"Karachi","KHI","Abu Dhabi","AUH","Etihad","2h 30m",56000,112000,220000,440000,"PKR"},
            {"Karachi","KHI","Abu Dhabi","AUH","PIA","2h 30m",50000,100000,190000,380000,"PKR"},
            {"Islamabad","ISB","Abu Dhabi","AUH","Etihad","3h 40m",62000,124000,230000,460000,"PKR"},
            {"Lahore","LHE","Abu Dhabi","AUH","Etihad","3h 20m",60000,120000,225000,450000,"PKR"},

            // ─── Pakistan → Doha ──────────────────────────────────────────
            {"Karachi","KHI","Doha","DOH","Qatar Airways","3h 00m",62000,124000,240000,480000,"PKR"},
            {"Karachi","KHI","Doha","DOH","PIA","3h 00m",55000,110000,200000,400000,"PKR"},
            {"Islamabad","ISB","Doha","DOH","Qatar Airways","4h 05m",68000,136000,250000,500000,"PKR"},
            {"Lahore","LHE","Doha","DOH","Qatar Airways","3h 45m",65000,130000,245000,490000,"PKR"},

            // ─── Pakistan → Riyadh ────────────────────────────────────────
            {"Karachi","KHI","Riyadh","RUH","PIA","3h 30m",58000,116000,210000,420000,"PKR"},
            {"Karachi","KHI","Riyadh","RUH","Saudi Airlines","3h 30m",62000,124000,225000,450000,"PKR"},
            {"Islamabad","ISB","Riyadh","RUH","PIA","4h 10m",65000,130000,220000,440000,"PKR"},
            {"Lahore","LHE","Riyadh","RUH","PIA","3h 50m",60000,120000,215000,430000,"PKR"},

            // ─── Pakistan → London ────────────────────────────────────────
            {"Karachi","KHI","London","LHR","PIA","8h 30m",145000,290000,420000,840000,"PKR"},
            {"Karachi","KHI","London","LHR","Emirates","9h 30m",165000,330000,480000,960000,"PKR"},
            {"Karachi","KHI","London","LHR","Qatar Airways","10h 00m",155000,310000,450000,900000,"PKR"},
            {"Islamabad","ISB","London","LHR","PIA","7h 50m",138000,276000,410000,820000,"PKR"},
            {"Islamabad","ISB","London","LHR","Turkish Airlines","10h 30m",148000,296000,435000,870000,"PKR"},
            {"Lahore","LHE","London","LHR","PIA","8h 10m",140000,280000,415000,830000,"PKR"},

            // ─── Pakistan → Istanbul ─────────────────────────────────────
            {"Karachi","KHI","Istanbul","IST","Turkish Airlines","7h 00m",105000,210000,360000,720000,"PKR"},
            {"Karachi","KHI","Istanbul","IST","Qatar Airways","9h 30m",115000,230000,380000,760000,"PKR"},
            {"Islamabad","ISB","Istanbul","IST","Turkish Airlines","6h 00m",98000,196000,345000,690000,"PKR"},
            {"Lahore","LHE","Istanbul","IST","Turkish Airlines","6h 30m",100000,200000,350000,700000,"PKR"},

            // ─── Pakistan → Toronto ───────────────────────────────────────
            {"Karachi","KHI","Toronto","YYZ","Etihad","16h 00m",210000,420000,680000,1360000,"PKR"},
            {"Karachi","KHI","Toronto","YYZ","Qatar Airways","18h 00m",225000,450000,710000,1420000,"PKR"},
            {"Islamabad","ISB","Toronto","YYZ","Turkish Airlines","17h 00m",215000,430000,695000,1390000,"PKR"},
            {"Lahore","LHE","Toronto","YYZ","Etihad","16h 30m",218000,436000,685000,1370000,"PKR"},

            // ─── Pakistan → New York ──────────────────────────────────────
            {"Karachi","KHI","New York","JFK","Emirates","16h 30m",245000,490000,750000,1500000,"PKR"},
            {"Karachi","KHI","New York","JFK","Qatar Airways","17h 00m",255000,510000,770000,1540000,"PKR"},
            {"Islamabad","ISB","New York","JFK","Turkish Airlines","16h 00m",240000,480000,740000,1480000,"PKR"},
            {"Lahore","LHE","New York","JFK","Emirates","15h 45m",248000,496000,755000,1510000,"PKR"},

            // ─── Pakistan → Beijing ───────────────────────────────────────
            {"Karachi","KHI","Beijing","PEK","PIA","5h 30m",98000,196000,280000,560000,"PKR"},
            {"Islamabad","ISB","Beijing","PEK","PIA","4h 45m",92000,184000,265000,530000,"PKR"},
            {"Lahore","LHE","Beijing","PEK","PIA","5h 10m",95000,190000,272000,544000,"PKR"},

            // ─── Pakistan → Bangkok ───────────────────────────────────────
            {"Karachi","KHI","Bangkok","BKK","Thai Airways","5h 00m",88000,176000,265000,530000,"PKR"},
            {"Karachi","KHI","Bangkok","BKK","Emirates","8h 30m",95000,190000,280000,560000,"PKR"},
            {"Islamabad","ISB","Bangkok","BKK","Thai Airways","5h 30m",94000,188000,270000,540000,"PKR"},
            {"Lahore","LHE","Bangkok","BKK","Thai Airways","5h 15m",91000,182000,268000,536000,"PKR"},

            // ─── Pakistan → Kuala Lumpur ──────────────────────────────────
            {"Karachi","KHI","Kuala Lumpur","KUL","Malaysia Airlines","5h 30m",95000,190000,275000,550000,"PKR"},
            {"Karachi","KHI","Kuala Lumpur","KUL","AirAsia","6h 00m",82000,164000,0,0,"PKR"},
            {"Islamabad","ISB","Kuala Lumpur","KUL","Malaysia Airlines","6h 00m",100000,200000,280000,560000,"PKR"},
            {"Lahore","LHE","Kuala Lumpur","KUL","Malaysia Airlines","5h 45m",97000,194000,278000,556000,"PKR"},
        };

        for (int r = 0; r < rows.GetLength(0); r++)
        {
            for (int c = 0; c < rows.GetLength(1); c++)
                ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r, c]);
        }

        ws.Columns().AdjustToContents();
    }
}
