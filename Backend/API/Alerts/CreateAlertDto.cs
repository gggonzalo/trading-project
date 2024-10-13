public class CreateAlertDto
{
    public required string Symbol { get; set; }
    public required decimal ValueTarget { get; set; }
    public required Guid SubscriptionId { get; set; }
}
