
using Binance.Net.Enums;

public record TargetRsiOrderInstruction
{
    public Guid Id { get; init; }
    public required string OrderId { get; init; }
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public required decimal QuoteQty { get; init; }
    public required KlineInterval Interval { get; init; }
    public required decimal TargetRsi { get; init; }
}
