public record PriceFormat
{
    public required decimal MinMove { get; init; }
    public required int Precision { get; init; }
}

public record QuantityFormat
{
    public required decimal MinMove { get; init; }
    public required int Precision { get; init; }
}

public record SymbolInfo
{
    public required string Symbol { get; init; }
    public required string BaseAsset { get; init; }
    public required string QuoteAsset { get; init; }
    public required PriceFormat PriceFormat { get; init; }
    public required QuantityFormat QuantityFormat { get; init; }
}