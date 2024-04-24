using Binance.Net.Enums;

public record CreateTargetRsiOrderInstructionDto
{
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public required decimal QuoteQty { get; init; }
    public required KlineInterval Interval { get; init; }
    public required decimal TargetRsi { get; init; }
}