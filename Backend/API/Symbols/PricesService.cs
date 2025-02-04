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
}