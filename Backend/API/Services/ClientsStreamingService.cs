using Binance.Net.Enums;
using CryptoExchange.Net.Objects.Sockets;
using Microsoft.AspNetCore.SignalR;

public class ClientsStreamingService(IHubContext<CandlesHub> candlesHubContext, CandlesService candlesService)
{
    private Dictionary<string, UpdateSubscription> _clientsSubscriptions = [];

    public async Task StartCandleUpdatesStreamAsync(string clientConnectionId, IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals)
    {
        var subscription = await candlesService.SubscribeToCandleUpdatesAsync(symbols, intervals, c =>
        {
            candlesHubContext.Clients.Client(clientConnectionId).SendAsync("CandleUpdate", new
            {
                c.Symbol,
                c.Interval,
                Candle = new
                {
                    c.Candle.Time,
                    c.Candle.Open,
                    c.Candle.High,
                    c.Candle.Low,
                    c.Candle.Close,
                }
            });
        });

        _clientsSubscriptions[clientConnectionId] = subscription;
    }

    public async Task StopCandleUpdatesStreamAsync(string clientConnectionId)
    {
        if (_clientsSubscriptions.TryGetValue(clientConnectionId, out var subscription))
        {
            await candlesService.UnsubscribeFromCandleUpdatesAsync(subscription);

            _clientsSubscriptions.Remove(clientConnectionId);
        }
    }
}