using Binance.Net.Enums;

public record SymbolIntervalRsiCandle
{
    public required string Symbol { get; init; }
    public required KlineInterval Interval { get; init; }
    public RsiCandle? Candle { get; init; }
}