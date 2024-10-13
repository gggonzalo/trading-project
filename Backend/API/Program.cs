using System.Text.Json.Serialization;
using CryptoExchange.Net.Authentication;
using Microsoft.EntityFrameworkCore;
using Quartz;

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

// TODO: Remove credentials if no endpoint is using them
var configuration = builder.Configuration;
var binanceApiCredentials = new ApiCredentials(configuration["BinanceApi:Key"], configuration["BinanceApi:Secret"]);

builder.Services.AddBinance(restOptions =>
{
    restOptions.ApiCredentials = binanceApiCredentials;
}, socketOptions =>
{
    socketOptions.ApiCredentials = binanceApiCredentials;
});

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddQuartz(q =>
{
    // q.AddJob<TargetRsiOrdersJob>(j => j.WithIdentity("RsiOrdersJob"));

    // q.AddTrigger(t => t
    //     .ForJob("RsiOrdersJob")
    //     .WithCronSchedule("0 * * ? * *"));


    // q.AddJob<AlertsMonitoringJob>(j => j.WithIdentity("AlertsMonitoringJob"));

    // // TODO: Add a job for each interval. Try to pass the interval as a parameter to the job
    // q.AddTrigger(t => t
    //     .ForJob("AlertsMonitoringJob")
    //     .WithCronSchedule("0 * * ? * *"));
});
builder.Services.AddQuartzHostedService();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<IPushNotificationsService, OneSignalService>();

// TODO: Singleton works for now but it will make more sense when reuse streams for multiple clients
builder.Services.AddSingleton<CandlesService>();
builder.Services.AddSingleton<PriceService>();
builder.Services.AddSingleton<SymbolsService>();

builder.Services.AddSingleton<ClientsStreamingService>();

var app = builder.Build();

app.UseCors();

app.AddAlertsEndpoints();
app.AddCandlesEndpoints();
app.AddSymbolsEndpoints();

app.Run();


#region Unused code
// builder.Services.AddSingleton<RsiCandlesService>();

// // var ordersMonitorService = app.Services.GetRequiredService<OrdersMonitorService>();
// // await ordersMonitorService.StartMonitoring();

// app.MapGet("/target-rsi-order-instructions", async (string symbol, AppDbContext dbContext) =>
// {
//     var instructions = await dbContext.TargetRsiOrderInstructions.Where(i => i.Symbol == symbol).ToListAsync();

//     return instructions;
// });

// // TODO: Handle concurrency issues with the job. For now, just check time when creating instructions
// app.MapPost("/target-rsi-order-instructions", async (CreateTargetRsiOrderInstructionDto targetRsiOrderInstruction, AppDbContext dbContext, IBinanceRestClient binanceRestClient, BinanceUtilities binanceUtilities) =>
// {
//     // Creating the instruction
//     var instruction = new TargetRsiOrderInstruction
//     {
//         OrderId = "TROI_" + Nanoid.Generate(size: 30),
//         Symbol = targetRsiOrderInstruction.Symbol,
//         Side = targetRsiOrderInstruction.Side,
//         QuoteQty = targetRsiOrderInstruction.QuoteQty,
//         Interval = targetRsiOrderInstruction.Interval,
//         TargetRsi = targetRsiOrderInstruction.TargetRsi,
//     };

//     dbContext.TargetRsiOrderInstructions.Add(instruction);

//     // Creating the order
//     // TODO: Move this method to candles service
//     var closedKlinesUntilNow = await binanceUtilities.GetClosedKlinesUntilNow(
//         instruction.Symbol,
//         instruction.Interval,
//         DateTimeOffset.UtcNow);
//     var closedClosePrices = closedKlinesUntilNow.Select(k => k.ClosePrice).ToList();

//     var (priceDecimalPlaces, quantityDecimalPlaces) = await binanceUtilities.GetSymbolPriceAndQuantityDecimalPlaces(instruction.Symbol);

//     var priceForTargetRsi = Math.Round(RsiCalculatorService.GetPriceForTargetRsi(closedClosePrices, instruction.TargetRsi), priceDecimalPlaces);
//     var baseAssetQuantityFromTargetPrice = Math.Round(instruction.QuoteQty / priceForTargetRsi, quantityDecimalPlaces);

//     // TODO: Keep as reference if we need futures in the future, haha
//     // 'Commiting' both changes
//     // await binanceRestClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(instruction.Symbol, FuturesMarginType.Isolated);
//     // await binanceRestClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(instruction.Symbol, 1);

//     var placedOrderResult = await binanceRestClient.SpotApi.Trading.PlaceOrderAsync(
//         instruction.Symbol,
//         instruction.Side,
//         SpotOrderType.Limit,
//         quantity: baseAssetQuantityFromTargetPrice,
//         price: priceForTargetRsi,
//         timeInForce: TimeInForce.GoodTillCanceled,
//         newClientOrderId: instruction.OrderId);

//     if (!placedOrderResult.Success) return Results.BadRequest(placedOrderResult.Error);

//     await dbContext.SaveChangesAsync();

//     return Results.Ok();
// });

// // Add DELETE endpoint to delete target rsi order instruction and cancel it from binance
// app.MapDelete("/target-rsi-order-instructions/{instructionId}", async (Guid instructionId, string symbol, AppDbContext dbContext, IBinanceRestClient binanceRestClient) =>
// {
//     var instruction = await dbContext.TargetRsiOrderInstructions.FindAsync(instructionId);

//     if (instruction == null)
//     {
//         return Results.NotFound();
//     }

//     var cancelOrderResult = await binanceRestClient.SpotApi.Trading.CancelOrderAsync(symbol, origClientOrderId: instruction.OrderId);

//     dbContext.TargetRsiOrderInstructions.Remove(instruction);
//     await dbContext.SaveChangesAsync();

//     return Results.Ok();
// });

// app.MapHub<RsiCandlesHub>("/rsi-candles");
#endregion