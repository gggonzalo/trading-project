public interface IPushNotificationsService
{
    Task SendNotificationAsync(string message);
}