using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Polly;

public class BinanceUtilities(IBinanceRestClient binanceRestClient)
{
    public async Task<IEnumerable<IBinanceKline>> GetClosedKlinesUntilNow(string symbol, KlineInterval interval, DateTimeOffset currentTime)
    {
        try
        {
            var getKlinesRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(2));

            var klines = await getKlinesRetryPolicy.ExecuteAsync(async () =>
             {
                 var klinesResult = await binanceRestClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval);

                 var klines = klinesResult.Data;

                 if (klines.Last().CloseTime <= currentTime)
                 {
                     throw new Exception("The last kline close time must be greater than the current time so that we know it is open.");
                 }

                 return klines;
             });

            // Removing the last/current kline because it is still open
            return klines.Take(klines.Count() - 1);
        }
        catch
        {
            // TODO: Save error result to the database
            Console.WriteLine("Failed to fetch klines to place target RSI order.");

            throw;
        }
    }

    public async Task<(int, int)> GetSymbolPriceAndQuantityDecimalPlaces(string symbol)
    {
        // TODO: Improve this
        var exchangeInfoResult = await binanceRestClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
        var symbolInfo = exchangeInfoResult.Data.Symbols.First(s => s.Name == symbol);

        var priceIncrement = symbolInfo.PriceFilter!.TickSize.ToString().TrimEnd('0');
        var quantityIncrement = symbolInfo.LotSizeFilter!.StepSize.ToString().TrimEnd('0');

        var priceDecimalPlaces = priceIncrement.IndexOf('.') == -1 ? 0 : priceIncrement.Length - priceIncrement.IndexOf('.') - 1;
        var quantityDecimalPlaces = quantityIncrement.IndexOf('.') == -1 ? 0 : quantityIncrement.Length - quantityIncrement.IndexOf('.') - 1;

        return (priceDecimalPlaces, quantityDecimalPlaces);
    }
}