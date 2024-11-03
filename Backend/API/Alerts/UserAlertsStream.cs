using Binance.Net.Enums;
using CryptoExchange.Net.Objects.Sockets;

public class UserAlertsStream(
    List<Alert> alerts,
    IPushNotificationsService pushNotificationsService,
    AppDbContext dbContext,
    CandlesService candlesService
)
{
    private List<Alert> _alerts = alerts;
    private UpdateSubscription? _candlesSubscription = null;

    public void AddOrUpdateAlert(Alert alert)
    {
        var existingAlert = _alerts.FirstOrDefault(a => a.Id == alert.Id);

        if (existingAlert != null)
        {
            _alerts.Remove(existingAlert);
        }

        _alerts.Add(alert);

        UpdateStream();
    }

    public void RemoveAlert(Guid alertId)
    {
        var alert = _alerts.FirstOrDefault(a => a.Id == alertId);

        if (alert != null)
        {
            _alerts.Remove(alert);

            UpdateStream();
        }
    }

    private async void UpdateStream()
    {
        // Close the existing stream if there is one
        _candlesSubscription?.CloseAsync();


        // Open a new stream with updated symbols if there are active alerts
        if (_alerts.Any())
        {
            var alertsSymbols = _alerts.Select(a => a.Symbol).Distinct().ToList();

            _candlesSubscription = await candlesService.SubscribeToCandleUpdatesAsync(
                alertsSymbols,
                [KlineInterval.OneMinute],
                OnPriceUpdate
            );
        }
    }

    private async void OnPriceUpdate(SymbolIntervalCandle symbolIntervalCandle)
    {
        var candle = symbolIntervalCandle.Candle;
        var alertsForSymbol = _alerts.Where(a => a.Symbol == symbolIntervalCandle.Symbol).ToList();

        foreach (var alert in alertsForSymbol)
        {
            var isBearishAlert = alert.ValueTarget <= alert.ValueOnCreation;

            // Bearish alert
            if (isBearishAlert && candle.Close <= alert.ValueTarget)
            {
                await pushNotificationsService.SendNotificationAsync(alert.SubscriptionId, $"{alert.Symbol} price dropped to {alert.ValueTarget}!");

                if (alert.Trigger == TriggerType.OnlyOnce)
                {
                    dbContext.Alerts.Remove(alert);
                    await dbContext.SaveChangesAsync();
                }
            }
            // Bullish alert
            else if (candle.Close >= alert.ValueTarget)
            {
                await pushNotificationsService.SendNotificationAsync(alert.SubscriptionId, $"{alert.Symbol} price rose to {alert.ValueTarget}!");

                if (alert.Trigger == TriggerType.OnlyOnce)
                {
                    dbContext.Alerts.Remove(alert);
                    await dbContext.SaveChangesAsync();
                }
            }

        }
    }
}
