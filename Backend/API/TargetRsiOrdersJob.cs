using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Microsoft.EntityFrameworkCore;
using NanoidDotNet;
using Polly;
using Quartz;

public class TargetRsiOrdersJob(IBinanceRestClient binanceRestClient, AppDbContext appDbContext, BinanceUtilities binanceUtilities) : IJob
{
    private readonly IBinanceRestClient _binanceRestClient = binanceRestClient;
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly BinanceUtilities _binanceUtilities = binanceUtilities;

    public async Task Execute(IJobExecutionContext context)
    {
        var jobFireTime = context.ScheduledFireTimeUtc!.Value;

        var targetRsiOrderInstructions = await _appDbContext.TargetRsiOrderInstructions.ToListAsync();
        var targetRsiOrderInstructionsToCheck = targetRsiOrderInstructions.Where(instruction => ShouldCheckInstruction(instruction, jobFireTime));

        if (!targetRsiOrderInstructionsToCheck.Any())
        {
            return;
        }

        var openOrdersResult = await _binanceRestClient.UsdFuturesApi.Trading.GetOpenOrdersAsync();
        var openTargetRsiOrders = openOrdersResult.Data.Where(o => o.ClientOrderId.StartsWith("TROI_"));

        foreach (var instruction in targetRsiOrderInstructionsToCheck)
        {
            var instructedOrder = openTargetRsiOrders.FirstOrDefault(o => o.ClientOrderId == instruction.OrderId);

            // This will happen if the order was filled
            if (instructedOrder == null) continue;

            // TODO: Group by instruction symbol and interval and fetch klines for each instruction group to and use it for all the items in the group
            var closedKlinesUntilNow = await _binanceUtilities.GetClosedKlinesUntilNow(
                instruction.Symbol,
                instruction.Interval,
                jobFireTime
            );
            var closePrices = closedKlinesUntilNow.Select(k => k.ClosePrice).ToList();

            var (priceDecimalPlaces, quantityDecimalPlaces) = await _binanceUtilities.GetSymbolPriceAndQuantityDecimalPlaces(instruction.Symbol);

            var priceForTargetRsi = Math.Round(RsiCalculatorService.GetPriceForTargetRsi(closePrices, instruction.TargetRsi), priceDecimalPlaces);
            var baseAssetQuantityFromTargetPrice = Math.Round(instruction.QuoteQty / priceForTargetRsi, quantityDecimalPlaces);

            // TODO: Improve this catching of exceptions
            try
            {
                // Order already exists so we need to update it
                if (instructedOrder.Status == OrderStatus.New)
                {
                    // TODO: Handle CancelReplaceMode (Spot) and cancel/place errors (Futures)
                    await _binanceRestClient.UsdFuturesApi.Trading.CancelOrderAsync(instructedOrder.Symbol, origClientOrderId: instructedOrder.ClientOrderId);

                    await binanceRestClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(instruction.Symbol, FuturesMarginType.Isolated);
                    await binanceRestClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(instruction.Symbol, 1);

                    await _binanceRestClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                        instructedOrder.Symbol,
                        instructedOrder.Side,
                        FuturesOrderType.Limit,
                        quantity: baseAssetQuantityFromTargetPrice,
                        price: priceForTargetRsi,
                        timeInForce: TimeInForce.GoodTillCanceled,
                        newClientOrderId: instructedOrder.ClientOrderId);

                    continue;
                }
            }
            catch (Exception e)
            {

                throw;
            }
        }
    }

    // We only want to check a instruction when a new kline is opened for its respective interval
    private bool ShouldCheckInstruction(TargetRsiOrderInstruction instruction, DateTimeOffset scheduledFireTimeUtc)
    {
        var interval = instruction.Interval;
        var scheduledTime = scheduledFireTimeUtc.UtcDateTime;

        switch (interval)
        {
            case KlineInterval.OneMinute:
                return scheduledTime.Second == 0;

            case KlineInterval.FiveMinutes:
                return scheduledTime.Minute % 5 == 0 && scheduledTime.Second == 0;

            case KlineInterval.FifteenMinutes:
                return scheduledTime.Minute % 15 == 0 && scheduledTime.Second == 0;

            case KlineInterval.OneHour:
                return scheduledTime.Minute == 0 && scheduledTime.Second == 0;

            case KlineInterval.FourHour:
                return scheduledTime.Hour % 4 == 0 && scheduledTime.Minute == 0 && scheduledTime.Second == 0;

            case KlineInterval.OneDay:
                return scheduledTime.Hour == 0 && scheduledTime.Minute == 0 && scheduledTime.Second == 0;

            case KlineInterval.OneWeek:
                return scheduledTime.DayOfWeek == DayOfWeek.Monday && scheduledTime.Hour == 0 && scheduledTime.Minute == 0 && scheduledTime.Second == 0;

            case KlineInterval.OneMonth:
                return scheduledTime.Day == 1 && scheduledTime.Hour == 0 && scheduledTime.Minute == 0 && scheduledTime.Second == 0;

            default:
                return false;
        }
    }
}