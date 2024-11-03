public class AlertsManager(
    UserAlertsStreamFactory userAlertsStreamFactory
) : IUserAlertsManager
{
    private readonly Dictionary<Guid, UserAlertsStream> _userAlertsStreams = [];

    public void StartAlert(Alert alert)
    {
        if (!_userAlertsStreams.ContainsKey(alert.SubscriptionId))
        {
            var userAlertsStream = userAlertsStreamFactory.Create(alert.SubscriptionId);
            _userAlertsStreams.Add(alert.SubscriptionId, userAlertsStream);
        }

        _userAlertsStreams[alert.SubscriptionId].AddOrUpdateAlert(alert);
    }

    public void StopAlert(Guid subscriptionId, Guid alertId)
    {
        if (_userAlertsStreams.ContainsKey(subscriptionId))
        {
            _userAlertsStreams[subscriptionId].RemoveAlert(alertId);
        }
    }
}
