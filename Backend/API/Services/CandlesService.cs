using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Objects.Sockets;

public class CandlesService(IBinanceRestClient binanceRestClient, IBinanceSocketClient binanceSocketClient)
{
    // TODO: Implement same methods for spot

    public async Task<IEnumerable<Candle>> GetCandlesAsync(string symbol, KlineInterval interval)
    {
        var klinesResult = await binanceRestClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: 1000);
        var klines = klinesResult.Data;

        return klines.Select(k => new Candle
        {
            Time = k.OpenTime,
            Open = k.OpenPrice,
            High = k.HighPrice,
            Low = k.LowPrice,
            Close = k.ClosePrice,
        });
    }

    public async Task<UpdateSubscription> SubscribeToCandleUpdates(IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals, Action<SymbolIntervalCandle> onCandleUpdate)
    {
        var subscriptionResult = await binanceSocketClient.UsdFuturesApi.SubscribeToKlineUpdatesAsync(
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
                        Time = kline.OpenTime,
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

    public async Task UnsubscribeFromCandleUpdates(UpdateSubscription subscription)
    {
        await binanceSocketClient.UsdFuturesApi.UnsubscribeAsync(subscription);
    }
}