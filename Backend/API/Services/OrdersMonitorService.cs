using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects.Sockets;
using NanoidDotNet;


public class OrdersMonitorService(IBinanceSocketClient socketClient)
{
    public async Task StartMonitoring()
    {
        var listenKeyResponse = await socketClient.SpotApi.Account.StartUserStreamAsync();
        var listenKey = listenKeyResponse.Data.Result;

        await socketClient.SpotApi.Account.SubscribeToUserDataUpdatesAsync(listenKey, HandleOrderUpdate);

        // The listen key will stay valid for 60 minutes, after this no updates will be send anymore. To extend the life time of the listen key it is recommended to call the KeepAliveUserStreamAsync method every 30 minutes
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(30));

                await socketClient.SpotApi.Account.KeepAliveUserStreamAsync(listenKey);
            }
        });
    }

    private void HandleOrderUpdate(DataEvent<BinanceStreamOrderUpdate> orderUpdateEvent)
    {
        var orderUpdate = orderUpdateEvent.Data;

        // Check if the client order id starts with "TROI_" to identify the target RSI orders and place a profit order
        if (orderUpdate.ClientOrderId.StartsWith("TROI_") && orderUpdate.Status == OrderStatus.Filled)
        {
            var profitOrderSide = orderUpdate.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            var profitOrderPrice = orderUpdate.Price * (profitOrderSide == OrderSide.Sell ? 1.0075m : 0.9925m);

            socketClient.SpotApi.Trading.PlaceOrderAsync(
                orderUpdate.Symbol,
                profitOrderSide,
                SpotOrderType.Limit,
                quantity: orderUpdate.Quantity,
                price: profitOrderPrice,
                timeInForce: TimeInForce.GoodTillCanceled,
                newClientOrderId: "ACCU_" + Nanoid.Generate(size: 30));
        }
    }
}