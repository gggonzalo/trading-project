public record RsiCandle
{
    public required double Time { get; init; }
    public decimal? High { get; init; }
    public decimal? Low { get; init; }
    public decimal? Close { get; init; }
}
