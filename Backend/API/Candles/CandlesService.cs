using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Objects.Sockets;

// TODO: Extract to interface so we can have data from different sources
public class CandlesService(IBinanceRestClient binanceRestClient, IBinanceSocketClient binanceSocketClient)
{
    public async Task<IEnumerable<Candle>> GetCandlesAsync(string symbol, KlineInterval interval, DateTime? startTime = null, DateTime? endTime = null, int? limit = 1000)
    {
        var klinesResult = await binanceRestClient.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, startTime, endTime, limit);
        var klines = klinesResult.Data;

        return klines.Select(k => new Candle
        {
            Time = k.OpenTime.ToUnixEpoch(),
            Open = k.OpenPrice,
            High = k.HighPrice,
            Low = k.LowPrice,
            Close = k.ClosePrice,
        });
    }

    public async Task<UpdateSubscription> SubscribeToCandleUpdatesAsync(IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals, Action<SymbolIntervalCandle> onCandleUpdate)
    {
        var subscriptionResult = await binanceSocketClient.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(
            symbols,
            intervals,
            e =>
            {
                var klineData = e.Data;
                var kline = e.Data.Data;

                onCandleUpdate(new SymbolIntervalCandle
                {
                    Symbol = klineData.Symbol,
                    Interval = kline.Interval,
                    Candle = new Candle
                    {
                        Time = kline.OpenTime.ToUnixEpoch(),
                        Open = kline.OpenPrice,
                        High = kline.HighPrice,
                        Low = kline.LowPrice,
                        Close = kline.ClosePrice,
                    }
                });
            });

        if (!subscriptionResult.Success)
        {
            throw new Exception($"An error occured when subscribing to candle updates: {subscriptionResult.Error?.Message ?? "No error message provided"}.");
        }

        return subscriptionResult.Data;
    }

    public async Task UnsubscribeFromCandleUpdatesAsync(UpdateSubscription subscription)
    {
        await binanceSocketClient.SpotApi.UnsubscribeAsync(subscription);
    }
}