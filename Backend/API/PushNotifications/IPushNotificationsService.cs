public interface IPushNotificationsService
{
    Task SendNotificationAsync(Guid subscriptionId, string message);
}