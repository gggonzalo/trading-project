using RestSharp;

// REST API reference => https://documentation.onesignal.com/reference/rest-api-overview
public class OneSignalService(IConfiguration configuration) : IPushNotificationsService
{
    private readonly IConfiguration _configuration = configuration;

    public async Task SendNotificationAsync(Guid subscriptionId, string message)
    {
        var clientOptions = new RestClientOptions
        {
            BaseUrl = new Uri("https://onesignal.com/api/v1")
        };
        var client = new RestClient(clientOptions);

        var request = new RestRequest("/notifications", Method.Post);

        request.AddHeader("Authorization", $"Basic {_configuration["OneSignal:ApiKey"]}");
        request.AddHeader("Content-Type", "application/json");

        var notification = new
        {
            app_id = _configuration["OneSignal:AppId"],
            contents = new { en = message },
            include_subscription_ids = new[] { subscriptionId }
        };

        request.AddJsonBody(notification);

        await client.ExecuteAsync(request);
    }
}