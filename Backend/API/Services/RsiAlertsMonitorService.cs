// using Binance.Net.Enums;

// public class RsiAlertsMonitorService(RsiCandlesService rsiCandlesService, IPushNotificationsService pushNotificationsService)
// {

//     public async Task StartMonitoring()
//     {
//         var subscription = await rsiCandlesService.SubscribeToCandleUpdates(
//             new[] { "HBAR" },
//             new[] { KlineInterval.OneMinute },
//             async c =>
//             {
//                 if (c.Candle.Rsi.HasValue && c.Candle.Rsi.Value > 70)
//                 {
//                     await pushNotificationsService.SendNotificationAsync("BTCUSDT RSI is overbought");
//                 }
//                 else if (c.Candle.Rsi.HasValue && c.Candle.Rsi.Value < 30)
//                 {
//                     await pushNotificationsService.SendNotificationAsync("BTCUSDT RSI is oversold");
//                 }
//             }
//         );
//     }
// }