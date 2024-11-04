using Binance.Net.Enums;
using CryptoExchange.Net.Objects.Sockets;
using Microsoft.EntityFrameworkCore;

public class AlertsStream(
    IPushNotificationsService pushNotificationsService,
    IServiceScopeFactory scopeFactory,
    CandlesService candlesService
)
{
    private readonly List<Alert> _alerts = [];
    private UpdateSubscription? _candlesSubscription = null;

    public void AddOrUpdateAlerts(Alert alert)
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
        }

        UpdateStream();
    }

    private async void UpdateStream()
    {
        // Close the existing stream if there is one
        _candlesSubscription?.CloseAsync();


        // Open a new stream with updated symbols if there are active alerts
        if (_alerts.Count != 0)
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
        var activeAlertsForSymbol = _alerts.Where(a => a.Symbol == symbolIntervalCandle.Symbol && a.Status == AlertStatus.Active).ToList();

        foreach (var alert in activeAlertsForSymbol)
        {
            var isBearishAlert = alert.ValueTarget <= alert.ValueOnCreation;

            // Bearish alert
            if (isBearishAlert && candle.Close <= alert.ValueTarget)
            {
                await pushNotificationsService.SendNotificationAsync(alert.SubscriptionId, $"{alert.Symbol} price dropped to {alert.ValueTarget}!");

                if (alert.Trigger == AlertTrigger.OnlyOnce)
                {
                    alert.Status = AlertStatus.Triggered;

                    using var scope = scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await dbContext.Alerts.Where(a => a.Id == alert.Id).ExecuteUpdateAsync(b =>
                        b.SetProperty(a => a.Status, AlertStatus.Triggered)
                    );
                }
            }
            // Bullish alert
            else if (candle.Close >= alert.ValueTarget)
            {
                await pushNotificationsService.SendNotificationAsync(alert.SubscriptionId, $"{alert.Symbol} price rose to {alert.ValueTarget}!");

                if (alert.Trigger == AlertTrigger.OnlyOnce)
                {
                    alert.Status = AlertStatus.Triggered;

                    using var scope = scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await dbContext.Alerts.Where(a => a.Id == alert.Id).ExecuteUpdateAsync(b =>
                        b.SetProperty(a => a.Status, AlertStatus.Triggered)
                    );
                }
            }
        }
    }
}
