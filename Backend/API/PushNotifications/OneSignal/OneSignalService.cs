using System.Text.Json;
using RestSharp;

// REST API reference => https://documentation.onesignal.com/reference/rest-api-overview
public class OneSignalService(IConfiguration configuration) : IPushNotificationsService
{
    private readonly IConfiguration _configuration = configuration;

    public async Task<bool> IsSubscriptionValidAsync(Guid subscriptionId)
    {
        var clientOptions = new RestClientOptions
        {
            BaseUrl = new Uri("https://onesignal.com/api/v1")
        };
        var client = new RestClient(clientOptions);

        var request = new RestRequest("/apps/{AppId}/subscriptions/{SubscriptionId}/user/identity", Method.Get);

        request.AddHeader("Authorization", $"Basic {_configuration["OneSignal:ApiKey"]}");

        request.AddUrlSegment("AppId", _configuration["OneSignal:AppId"]!);
        request.AddUrlSegment("SubscriptionId", subscriptionId);

        var response = await client.ExecuteGetAsync(request);

        if (!response.IsSuccessful)
        {
            return false;
        }

        var identityBySubscriptionIdResponse = JsonSerializer.Deserialize<IdentityBySubscriptionIdResponse>(response.Content!);
        var userExistsForSubscriptionId = identityBySubscriptionIdResponse?.Identity.OneSignalId is not null;

        return userExistsForSubscriptionId;
    }

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