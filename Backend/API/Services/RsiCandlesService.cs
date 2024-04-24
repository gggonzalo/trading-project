using Binance.Net.Enums;
using CryptoExchange.Net.Objects.Sockets;

// TODO: Rename
public class RsiCandlesService(CandlesService candlesService)
{
    // TODO: Implement if we need to get the candles alone (not calculating them with the prices)
    // public async Task<IEnumerable<Candle>> GetCandlesAsync(string symbol, KlineInterval interval)
    // {
    // }

    public async Task<UpdateSubscription> SubscribeToCandleUpdates(IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals, Action<SymbolIntervalRsiCandle> onCandleUpdate)
    {
        // Create a RsiSeries for each symbol and interval
        var symbolIntervalRsiSeries = new Dictionary<string, Dictionary<KlineInterval, RsiSeries>>();
        var candlesTasks = new List<Task>();

        foreach (var symbol in symbols)
        {
            symbolIntervalRsiSeries[symbol] = [];

            foreach (var interval in intervals)
            {
                candlesTasks.Add(Task.Run(async () =>
                {
                    var candles = await candlesService.GetCandlesAsync(symbol, interval);

                    symbolIntervalRsiSeries[symbol][interval] = new RsiSeries(candles);
                }));
            }
        }

        await Task.WhenAll(candlesTasks);

        var subscription = await candlesService.SubscribeToCandleUpdates(
            symbols,
            intervals,
            c =>
            {
                var rsiSeries = symbolIntervalRsiSeries[c.Symbol][c.Interval];
                rsiSeries.Update(c.Candle);

                var lastRsiCandle = rsiSeries.GetLastRsiCandle();

                onCandleUpdate(new SymbolIntervalRsiCandle
                {
                    Symbol = c.Symbol,
                    Interval = c.Interval,
                    Candle = lastRsiCandle != null ? lastRsiCandle with
                    {
                        High = lastRsiCandle.High.HasValue ? Math.Round(lastRsiCandle.High.Value, 2) : null,
                        Low = lastRsiCandle.Low.HasValue ? Math.Round(lastRsiCandle.Low.Value, 2) : null,
                        Close = lastRsiCandle.Close.HasValue ? Math.Round(lastRsiCandle.Close.Value, 2) : null,
                    } : null
                });
            });

        return subscription;
    }

    public async Task UnsubscribeFromCandleUpdates(UpdateSubscription subscription)
    {
        await candlesService.UnsubscribeFromCandleUpdates(subscription);
    }
}