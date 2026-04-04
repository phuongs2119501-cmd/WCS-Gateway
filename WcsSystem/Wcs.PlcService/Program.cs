using Wcs.PlcService.Models;
using Wcs.PlcService.Plc;
using Wcs.PlcService.Services;

var builder = WebApplication.CreateBuilder(args);

//
// 🔹 Cho phép truy cập từ máy khác trong LAN
//
builder.WebHost.UseUrls("http://0.0.0.0:5000");

//
// 🔹 CORS — cho phép monitor.html (file://) gọi API
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

//
// 🔹 Xóa MVC, chỉ dùng Web API
//
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase);

//
// 1️⃣ PLC1
//
builder.Services.AddSingleton<Plc1Connector>(sp =>
{
	var settings = builder.Configuration
		.GetSection("Plc1Settings")
		.Get<PlcSettings>()!;

	return new Plc1Connector(settings);
});

//
// 2️⃣ PLC2
//
builder.Services.AddSingleton<Plc2Connector>(sp =>
{
	var settings = builder.Configuration
		.GetSection("Plc2Settings")
		.Get<PlcSettings>()!;

	return new Plc2Connector(settings);
});

//
// 3️⃣ GLOBAL STATE (UI sẽ đọc ở đây)
//
builder.Services.AddSingleton<SystemState>();

//
// 4️⃣ PLC SERVICES
builder.Services.AddSingleton<PlcBarcodeReader>();
builder.Services.AddSingleton<LocationRouter>();
builder.Services.AddSingleton<CraneService>();
builder.Services.AddSingleton<ShuttleService>();
builder.Services.AddSingleton<SystemService>();
builder.Services.AddSingleton<TcpClientService>();


//
// 5️⃣ Worker chạy nền
//
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();

app.MapControllers();

app.Run();