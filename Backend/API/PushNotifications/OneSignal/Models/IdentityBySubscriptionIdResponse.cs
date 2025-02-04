using System.Text.Json.Serialization;

public record Identity
{
    [JsonPropertyName("onesignal_id")]
    public Guid OneSignalId { get; set; }
}

public record IdentityBySubscriptionIdResponse
{
    [JsonPropertyName("identity")]
    public Identity Identity { get; set; }
}