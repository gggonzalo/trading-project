public interface IPushNotificationsService
{
    Task SendNotification(string message, List<string> playerIds);
}