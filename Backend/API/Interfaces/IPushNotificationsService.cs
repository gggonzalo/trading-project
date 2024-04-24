public interface IPushNotificationsService
{
    Task SendNotification(string message);
}