using Binance.Net.Enums;
using Microsoft.AspNetCore.SignalR;

public class RsiCandlesHub(ClientsStreamingService clientsStreamingService) : Hub
{
    public async Task SubscribeToRsiCandleUpdates(IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals)
    {
        var clientConnectionId = Context.ConnectionId;

        await clientsStreamingService.StartRsiCandlesStream(clientConnectionId, symbols, intervals);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var clientConnectionId = Context.ConnectionId;

        await clientsStreamingService.StopRsiCandlesStream(clientConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}