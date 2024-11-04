public class AlertsStreamFactory(
    IPushNotificationsService pushNotificationsService,
    AppDbContext dbContext,
    CandlesService candlesService
) : IAlertsStreamFactory
{
    public AlertsStream Create(Guid userSubscriptionId)
    {
        return new AlertsStream(pushNotificationsService, dbContext, candlesService);
    }
}
