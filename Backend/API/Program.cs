using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(opt =>
{
    opt.AddSimpleConsole(c =>
    {
        c.TimestampFormat = "[HH:mm:ss] ";
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=app.sqlite");
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddBinance();

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<IPushNotificationsService, OneSignalService>();
builder.Services.AddSingleton<IAlertsActivator, AlertsActivator>();
builder.Services.AddTransient<IAlertsStreamFactory, AlertsStreamFactory>();

builder.Services.AddTransient<CandlesService>();
builder.Services.AddTransient<PriceService>();
builder.Services.AddTransient<SymbolsService>();

builder.Services.AddSingleton<ClientsStreamingService>();

var app = builder.Build();

app.UseCors();

app.AddAlertsEndpoints();
app.AddCandlesEndpoints();
app.AddSymbolsEndpoints();

// #region Activate all active alerts
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

var alertsActivator = app.Services.GetRequiredService<IAlertsActivator>();

var activeAlerts = await dbContext.Alerts.Where(a => a.Status == AlertStatus.Active).ToListAsync();
alertsActivator.Activate(activeAlerts);
// #endregion

app.Run();
