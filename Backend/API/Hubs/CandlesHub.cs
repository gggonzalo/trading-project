using Binance.Net.Enums;
using Microsoft.AspNetCore.SignalR;

public class CandlesHub(ClientsStreamingService clientsStreamingService) : Hub
{
    public async Task SubscribeToCandleUpdates(IEnumerable<string> symbols, IEnumerable<KlineInterval> intervals)
    {
        var clientConnectionId = Context.ConnectionId;

        await clientsStreamingService.StartCandlesStream(clientConnectionId, symbols, intervals);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var clientConnectionId = Context.ConnectionId;

        await clientsStreamingService.StopCandlesStream(clientConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}