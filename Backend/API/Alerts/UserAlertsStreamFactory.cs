public class UserAlertsStreamFactory(
    IPushNotificationsService pushNotificationsService,
    CandlesService candlesService
) : IUserAlertsStreamFactory
{
    public UserAlertsStream Create(Guid userSubscriptionId)
    {
        return new UserAlertsStream(userSubscriptionId, pushNotificationsService, candlesService);
    }
}
