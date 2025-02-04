public interface IPushNotificationsService
{
    Task<bool> IsSubscriptionValidAsync(Guid subscriptionId);
    Task SendNotificationAsync(Guid subscriptionId, string message);
}