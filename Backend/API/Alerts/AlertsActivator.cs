public class AlertsActivator(
    IAlertsStreamFactory alertsStreamFactory
) : IAlertsActivator
{
    // Streams grouped by subscription id
    private readonly Dictionary<Guid, AlertsStream> _alertsStreams = [];

    public void Activate(IEnumerable<Alert> alerts)
    {
        foreach (var alert in alerts)
        {
            if (!_alertsStreams.TryGetValue(alert.SubscriptionId, out var alertsStream))
            {
                alertsStream = alertsStreamFactory.Create(alert.SubscriptionId);

                _alertsStreams[alert.SubscriptionId] = alertsStream;
            }

            alertsStream.AddOrUpdateAlerts(alert);
        }
    }

    public void Deactivate(Alert alert)
    {
        if (_alertsStreams.TryGetValue(alert.SubscriptionId, out var alertsStream))
        {
            alertsStream.RemoveAlert(alert.Id);
        }
    }
}
