using RestSharp;
using RestSharp.Authenticators;

public class OneSignalService(IConfiguration configuration) : IPushNotificationsService
{
    private readonly IConfiguration _configuration = configuration;

    public async Task SendNotification(string message, List<string> playerIds)
    {
        var clientOptions = new RestClientOptions
        {
            Authenticator = new HttpBasicAuthenticator(_configuration["OneSignal:ApiKey"], ""),
            BaseUrl = new Uri("https://onesignal.com/api/v1")
        };
        var client = new RestClient(clientOptions);

        var request = new RestRequest("/notifications", Method.Post);

        request.AddHeader("Content-Type", "application/json");

        var notification = new
        {
            app_id = _configuration["OneSignal:AppId"],
            contents = new { en = message },
        };

        request.AddJsonBody(notification);

        await client.ExecuteAsync(request);
    }
}