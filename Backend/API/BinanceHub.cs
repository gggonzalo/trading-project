using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.CommonObjects;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class BinanceHub : Hub
{
    // TODO: Use this hubcontext from outside
    private readonly IHubContext<BinanceHub> _hubContext;
    private readonly ConcurrentDictionary<string, ClientStreamsManager> _clients;

    public BinanceHub(IHubContext<BinanceHub> hubContext)
    {
        _hubContext = hubContext;
        _clients = new ConcurrentDictionary<string, ClientStreamsManager>();
    }

    public void SubscribeToKlineUpdates(string symbol, KlineInterval interval)
    {
        var clientConnectionId = Context.ConnectionId;

        var binanceSocketClientManager = new ClientStreamsManager(_hubContext, clientConnectionId);
        binanceSocketClientManager.SubscribeToKlineUpdates(symbol, interval);

        _clients.TryAdd(clientConnectionId, binanceSocketClientManager);
    }

    public async Task SubscribeToLastRsiCandleUpdates(IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals)
    {
        var clientConnectionId = Context.ConnectionId;

        var binanceSocketClientManager = new ClientStreamsManager(_hubContext, clientConnectionId);
        await binanceSocketClientManager.SubscribeToLastRsiCandleUpdates(symbols, intervals);

        _clients.TryAdd(clientConnectionId, binanceSocketClientManager);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (_clients.TryRemove(Context.ConnectionId, out var clientManager))
        {
            clientManager.Dispose();
        }

        return base.OnDisconnectedAsync(exception);
    }
}

public class ClientStreamsManager(IHubContext<BinanceHub> hubContext, string clientConnectionId) : IDisposable
{
    private readonly IHubContext<BinanceHub> _hubContext = hubContext;
    private readonly string _clientConnectionId = clientConnectionId;
    private readonly BinanceRestClient _binanceRestClient = new BinanceRestClient();
    private readonly BinanceSocketClient _binanceSocketClient = new BinanceSocketClient();

    public void SubscribeToKlineUpdates(string symbol, KlineInterval interval)
    {
        _ = _binanceSocketClient.UsdFuturesApi.SubscribeToKlineUpdatesAsync(
            symbol,
            interval,
            e =>
            {
                var kline = e.Data.Data;

                _hubContext.Clients.Client(_clientConnectionId).SendAsync("KlineUpdate", new
                {
                    Time = Utils.ToJavascriptSecs(kline.OpenTime),
                    Open = kline.OpenPrice,
                    High = kline.HighPrice,
                    Low = kline.LowPrice,
                    Close = kline.ClosePrice,
                });
            });
    }



    public async Task SubscribeToLastRsiCandleUpdates(IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals)
    {

        var symbolIntervalRsiCandlesServices = new Dictionary<string, Dictionary<KlineInterval, RsiCandlesService>>();

        // TODO: Use a more efficient way to initialize the dictionary in case klines are lost because of all the awaits and the late subscription
        foreach (var symbol in symbols)
        {
            symbolIntervalRsiCandlesServices.Add(symbol, []);


            foreach (var interval in intervals)
            {
                var klinesResult = await _binanceRestClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval);
                var klines = klinesResult.Data;

                var rsiCandlesService = new RsiCandlesService(klines);

                symbolIntervalRsiCandlesServices[symbol].Add(interval, rsiCandlesService);
            }
        }

        _ = _binanceSocketClient.UsdFuturesApi.SubscribeToKlineUpdatesAsync(
            symbols,
            intervals,
            e =>
            {
                var klineData = e.Data;
                var kline = e.Data.Data;

                var symbolIntervalRsiCandlesService = symbolIntervalRsiCandlesServices[klineData.Symbol][kline.Interval];
                symbolIntervalRsiCandlesService.Update(kline);

                var lastRsiCandle = symbolIntervalRsiCandlesService.GetLastRsiCandle();

                _hubContext.Clients.Client(_clientConnectionId).SendAsync("RsiCandleUpdate", new
                {
                    klineData.Symbol,
                    kline.Interval,
                    Candle = lastRsiCandle != null ? new
                    {
                        Time = Utils.ToJavascriptSecs(lastRsiCandle.Time),
                        High = Math.Round(lastRsiCandle.High, 2),
                        Low = Math.Round(lastRsiCandle.Low, 2),
                        Close = Math.Round(lastRsiCandle.Close, 2),
                    } : null
                });
            });
    }

    public void Dispose()
    {
        _binanceSocketClient.Dispose();

        GC.SuppressFinalize(this);
    }
}
