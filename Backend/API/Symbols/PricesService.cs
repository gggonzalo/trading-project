using Binance.Net.Interfaces.Clients;

// TODO: Extract to interface so we can have data from different sources
public class PriceService(IBinanceRestClient binanceRestClient)
{
    public async Task<PriceInfo> GetPriceAsync(string symbol)
    {
        var symbolPriceResult = await binanceRestClient.SpotApi.ExchangeData.GetPriceAsync(symbol);

        return new PriceInfo
        {
            Price = symbolPriceResult.Data.Price,
            Timestamp = symbolPriceResult.Data.Timestamp ?? DateTime.UtcNow
        };
    }

    // TODO: Remove if not needed
    // public async Task<IEnumerable<IBinanceKline>> GetClosedKlinesUntilNow(string symbol, KlineInterval interval, DateTimeOffset currentTime)
    // {
    //     try
    //     {
    //         var getKlinesRetryPolicy = Policy
    //             .Handle<Exception>()
    //             .WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(2));

    //         var klines = await getKlinesRetryPolicy.ExecuteAsync(async () =>
    //          {
    //              var klinesResult = await binanceRestClient.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval);

    //              var klines = klinesResult.Data;

    //              if (klines.Last().CloseTime <= currentTime)
    //              {
    //                  throw new Exception("The last kline close time must be greater than the current time so that we know it is open.");
    //              }

    //              return klines;
    //          });

    //         // Removing the last/current kline because it is still open
    //         return klines.Take(klines.Count() - 1);
    //     }
    //     catch
    //     {
    //         // TODO: Save error result to the database
    //         Console.WriteLine("Failed to fetch klines to place target RSI order.");

    //         throw;
    //     }
    // }
}