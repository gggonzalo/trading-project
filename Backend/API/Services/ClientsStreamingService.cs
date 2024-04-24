using Binance.Net.Enums;
using CryptoExchange.Net.Objects.Sockets;
using Microsoft.AspNetCore.SignalR;

public class ClientsStreamingService(IHubContext<CandlesHub> candlesHubContext, IHubContext<RsiCandlesHub> rsiCandlesHubContext, CandlesService candlesService, RsiCandlesService rsiCandlesService)
{
    private Dictionary<string, UpdateSubscription> _clientsSubscriptions = [];

    public async Task StartCandlesStream(string clientConnectionId, IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals)
    {
        var subscription = await candlesService.SubscribeToCandleUpdates(symbols, intervals, c =>
        {
            candlesHubContext.Clients.Client(clientConnectionId).SendAsync("CandleUpdate", new
            {
                c.Symbol,
                c.Interval,
                Candle = new
                {
                    Time = Utils.ToJavascriptSecs(c.Candle.Time),
                    c.Candle.Open,
                    c.Candle.High,
                    c.Candle.Low,
                    c.Candle.Close,
                }
            });
        });

        _clientsSubscriptions[clientConnectionId] = subscription;
    }

    public async Task StopCandlesStream(string clientConnectionId)
    {
        if (_clientsSubscriptions.TryGetValue(clientConnectionId, out var subscription))
        {
            await candlesService.UnsubscribeFromCandleUpdates(subscription);

            _clientsSubscriptions.Remove(clientConnectionId);
        }
    }

    public async Task StartRsiCandlesStream(string clientConnectionId, IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals)
    {
        var subscription = await rsiCandlesService.SubscribeToCandleUpdates(symbols, intervals, c =>
        {
            rsiCandlesHubContext.Clients.Client(clientConnectionId).SendAsync("RsiCandleUpdate", new
            {
                c.Symbol,
                c.Interval,
                Candle = c.Candle != null ? new
                {
                    Time = Utils.ToJavascriptSecs(c.Candle.Time),
                    c.Candle.High,
                    c.Candle.Low,
                    c.Candle.Close,
                } : null
            });
        });

        _clientsSubscriptions[clientConnectionId] = subscription;
    }

    public async Task StopRsiCandlesStream(string clientConnectionId)
    {
        if (_clientsSubscriptions.TryGetValue(clientConnectionId, out var subscription))
        {
            await rsiCandlesService.UnsubscribeFromCandleUpdates(subscription);

            _clientsSubscriptions.Remove(clientConnectionId);
        }
    }

}