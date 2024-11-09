using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
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
    _.AddPolicy("fixed-soft", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, partition =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    _.AddPolicy("fixed-medium", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, partition =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 4,
                Window = TimeSpan.FromSeconds(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    _.AddPolicy("fixed-hard", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, partition =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromSeconds(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    _.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.WriteAsJsonAsync("Too many requests. Please try again later.");

        return new ValueTask();
    };
});

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<IPushNotificationsService, OneSignalService>();
builder.Services.AddSingleton<ClientsStreamingService>();
builder.Services.AddSingleton<IAlertsActivator, AlertsActivator>();

builder.Services.AddTransient<IAlertsStreamFactory, AlertsStreamFactory>();
builder.Services.AddTransient<CandlesService>();
builder.Services.AddTransient<PriceService>();
builder.Services.AddTransient<SymbolsService>();


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
