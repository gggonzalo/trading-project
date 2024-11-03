
public enum TriggerType
{
    OnlyOnce,
    OncePerMinute,
}

public enum Status
{
    Active,
    Triggered,
}

public record Alert
{
    public Guid Id { get; init; }
    public required string Symbol { get; init; }
    public required decimal ValueOnCreation { get; init; }
    public required decimal ValueTarget { get; init; }
    public required TriggerType Trigger { get; init; }
    public required Status Status { get; init; }
    public required Guid SubscriptionId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}