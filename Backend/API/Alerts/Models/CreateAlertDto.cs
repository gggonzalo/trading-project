public record CreateAlertDto
{
    public required string Symbol { get; init; }
    public required decimal ValueTarget { get; init; }
    public required Guid SubscriptionId { get; init; }
}
