public record RsiCandle
{
    public required DateTime Time { get; init; }
    public decimal? High { get; init; }
    public decimal? Low { get; init; }
    public decimal? Close { get; init; }
}
