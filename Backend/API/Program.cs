using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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

builder.Services.AddRateLimiter(_ =>
{
    _.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    _.AddFixedWindowLimiter("fixed-soft", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromSeconds(10);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    _.AddFixedWindowLimiter("fixed-medium", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(10);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    _.AddFixedWindowLimiter("fixed-hard", options =>
    {
        options.PermitLimit = 3;
        options.Window = TimeSpan.FromSeconds(10);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<IPushNotificationsService, OneSignalService>();

builder.Services.AddTransient<IAlertsStreamFactory, AlertsStreamFactory>();
builder.Services.AddSingleton<IAlertsActivator, AlertsActivator>();

// TODO: Singleton works for now but it will make more sense when reuse streams for multiple clients
builder.Services.AddSingleton<CandlesService>();
builder.Services.AddSingleton<PriceService>();
builder.Services.AddSingleton<SymbolsService>();

builder.Services.AddSingleton<ClientsStreamingService>();

var app = builder.Build();

app.UseCors();
app.UseRateLimiter();

app.AddAlertsEndpoints();
app.AddCandlesEndpoints();
app.AddSymbolsEndpoints();

// Activate all active alerts
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

var alertsActivator = app.Services.GetRequiredService<IAlertsActivator>();

var activeAlerts = await dbContext.Alerts.Where(a => a.Status == AlertStatus.Active).ToListAsync();
alertsActivator.Activate(activeAlerts);

app.Run();
