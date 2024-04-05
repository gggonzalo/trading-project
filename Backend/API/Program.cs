using System.Text.Json.Serialization;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.EntityFrameworkCore;
using NanoidDotNet;
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
    q.AddJob<TargetRsiOrdersJob>(j => j.WithIdentity("RsiOrdersJob"));

    q.AddTrigger(t => t
        .ForJob("RsiOrdersJob")
        .WithCronSchedule("0 * * ? * *"));
});
builder.Services.AddQuartzHostedService();

builder.Services.AddSingleton<BinanceUtilities>();
builder.Services.AddSingleton<OrdersMonitorService>();

var app = builder.Build();

var ordersMonitorService = app.Services.GetRequiredService<OrdersMonitorService>();
// await ordersMonitorService.StartMonitoring();

app.UseCors();

app.MapGet("/symbols", async (IBinanceRestClient binanceRestClient, AppDbContext dbContext) =>
{
    // TODO: Replace hardcoded list with a list of symbols from the database
    var exchangeInfoResult = await binanceRestClient.SpotApi.ExchangeData.GetExchangeInfoAsync(["BTCUSDT", "ATOMUSDT", "ADAUSDT", "DOTUSDT", "HBARUSDT", "INJUSDT", "BOMEUSDT", "FLOKIUSDT", "WIFUSDT"]);
    var symbolsInfo = exchangeInfoResult.Data.Symbols;

    var symbolsWithNumOrders = await Task.WhenAll(
        symbolsInfo.Select(async i =>
            {
                var orderInstructionsCount = await dbContext.TargetRsiOrderInstructions.CountAsync(o => o.Symbol == i.Name);

                return new
                {
                    i.Name,
                    i.BaseAsset,
                    i.QuoteAsset,
                    PriceIncrement = i.PriceFilter?.TickSize,
                    QuantityIncrement = i.LotSizeFilter?.StepSize,
                    OrderInstructionsCount = orderInstructionsCount
                };
            }));

    // TODO: Add better mapping and remove unnecessary properties to not send them over the network
    return symbolsWithNumOrders;
});

app.MapGet("/klines", async (string symbol, KlineInterval interval, IBinanceRestClient binanceRestClient) =>
{
    var klinesResult = await binanceRestClient.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: 1000);
    var klines = klinesResult.Data;

    return klines.Select(k => new
    {
        Time = Utils.ToJavascriptSecs(k.OpenTime),
        Open = k.OpenPrice,
        High = k.HighPrice,
        Low = k.LowPrice,
        Close = k.ClosePrice,
    });
});


// app.MapPost("/order", async (OrderRequest orderRequest, IBinanceRestClient binanceRestClient) =>
// {
//     var placedOrderResult = await binanceRestClient.SpotApi.Trading.PlaceOrderAsync(orderRequest.Symbol, orderRequest.Side, orderRequest.Type, orderRequest.Quantity, price: orderRequest.Price, timeInForce: TimeInForce.GoodTillCanceled);

//     return placedOrderResult.Data;
// });


app.MapGet("/target-rsi-order-instructions", async (string symbol, AppDbContext dbContext) =>
{
    var instructions = await dbContext.TargetRsiOrderInstructions.Where(i => i.Symbol == symbol).ToListAsync();

    return instructions;
});

// TODO: Handle concurrency issues with the job. For now, just check time when creating instructions
app.MapPost("/target-rsi-order-instructions", async (CreateTargetRsiOrderInstructionDto targetRsiOrderInstruction, AppDbContext dbContext, IBinanceRestClient binanceRestClient, BinanceUtilities binanceUtilities) =>
{
    // Creating the instruction
    var instruction = new TargetRsiOrderInstruction
    {
        OrderId = "TROI_" + Nanoid.Generate(size: 30),
        Symbol = targetRsiOrderInstruction.Symbol,
        Side = targetRsiOrderInstruction.Side,
        QuoteQty = targetRsiOrderInstruction.QuoteQty,
        Interval = targetRsiOrderInstruction.Interval,
        TargetRsi = targetRsiOrderInstruction.TargetRsi,
    };

    dbContext.TargetRsiOrderInstructions.Add(instruction);

    // Creating the order
    var closedKlinesUntilNow = await binanceUtilities.GetClosedKlinesUntilNow(
        instruction.Symbol,
        instruction.Interval,
        DateTimeOffset.UtcNow);
    var closedClosePrices = closedKlinesUntilNow.Select(k => k.ClosePrice).ToList();

    var (priceDecimalPlaces, quantityDecimalPlaces) = await binanceUtilities.GetSymbolPriceAndQuantityDecimalPlaces(instruction.Symbol);

    var priceForTargetRsi = Math.Round(RsiCalculatorService.GetPriceForTargetRsi(closedClosePrices, instruction.TargetRsi), priceDecimalPlaces);
    var baseAssetQuantityFromTargetPrice = Math.Round(instruction.QuoteQty / priceForTargetRsi, quantityDecimalPlaces);

    // 'Commiting' both changes

    var placedOrderResult = await binanceRestClient.SpotApi.Trading.PlaceOrderAsync(
        instruction.Symbol,
        instruction.Side,
        SpotOrderType.Limit,
        quantity: baseAssetQuantityFromTargetPrice,
        price: priceForTargetRsi,
        timeInForce: TimeInForce.GoodTillCanceled,
        newClientOrderId: instruction.OrderId);

    if (!placedOrderResult.Success) return Results.BadRequest(placedOrderResult.Error);

    await dbContext.SaveChangesAsync();

    return Results.Ok();
});

// Add DELETE endpoint to delete target rsi order instruction and cancel it from binance
app.MapDelete("/target-rsi-order-instructions/{instructionId}", async (Guid instructionId, string symbol, AppDbContext dbContext, IBinanceRestClient binanceRestClient) =>
{
    var instruction = await dbContext.TargetRsiOrderInstructions.FindAsync(instructionId);

    if (instruction == null)
    {
        return Results.NotFound();
    }

    var cancelOrderResult = await binanceRestClient.SpotApi.Trading.CancelOrderAsync(symbol, origClientOrderId: instruction.OrderId);

    dbContext.TargetRsiOrderInstructions.Remove(instruction);
    await dbContext.SaveChangesAsync();

    return Results.Ok();
});

app.MapHub<BinanceHub>("/binance-hub");

app.Run();