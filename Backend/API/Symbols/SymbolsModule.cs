using Binance.Net.Interfaces.Clients;
using Microsoft.EntityFrameworkCore;

public static class SymbolsModule
{
    public static void AddSymbolsEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO: Make symbol info model
        app.MapGet("/symbols", async (IBinanceRestClient binanceRestClient, AppDbContext dbContext) =>
        {
            // TODO: Replace hardcoded list with a list of symbols from the database
            var symbols = new List<string> { "BTCUSDT", "ATOMUSDT", "ADAUSDT", "DOTUSDT", "HBARUSDT", "INJUSDT", "BOMEUSDT", "FLOKIUSDT", "WIFUSDT" };

            var exchangeInfoResult = await binanceRestClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
            var symbolsInfo = exchangeInfoResult.Data.Symbols.Where(s => symbols.Contains(s.Name));

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
    }
}