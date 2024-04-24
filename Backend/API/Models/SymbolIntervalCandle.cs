using Binance.Net.Enums;

public record SymbolIntervalCandle
{
    public required string Symbol { get; init; }
    public required KlineInterval Interval { get; init; }
    public required Candle Candle { get; init; }
}