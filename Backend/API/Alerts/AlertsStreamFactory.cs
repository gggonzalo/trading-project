public class AlertsStreamFactory(
    IPushNotificationsService pushNotificationsService,
    IServiceScopeFactory scopeFactory,
    CandlesService candlesService
) : IAlertsStreamFactory
{
    public AlertsStream Create(Guid userSubscriptionId)
    {
        return new AlertsStream(pushNotificationsService, scopeFactory, candlesService);
    }
}
