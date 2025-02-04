using Binance.Net.Objects.Models.Spot;

public static class BinanceSymbolExtensions
{
    public static PriceFormat GetPriceFormat(this BinanceSymbol symbolInfo)
    {
        if (symbolInfo.PriceFilter == null) return new PriceFormat
        {
            MinMove = 1M,
            Precision = 0
        };

        var minMoveStr = symbolInfo.PriceFilter.TickSize.ToString().TrimEnd('0');

        return new PriceFormat
        {
            MinMove = symbolInfo.PriceFilter.TickSize,
            Precision = minMoveStr.IndexOf('.') == -1 ? 0 : minMoveStr.Length - minMoveStr.IndexOf('.') - 1
        };
    }

    public static QuantityFormat GetQuantityFormat(this BinanceSymbol symbolInfo)
    {
        if (symbolInfo.LotSizeFilter == null) return new QuantityFormat
        {
            MinMove = 1M,
            Precision = 0
        };

        var minMoveStr = symbolInfo.LotSizeFilter.StepSize.ToString().TrimEnd('0');

        return new QuantityFormat
        {
            MinMove = symbolInfo.LotSizeFilter.StepSize,
            Precision = minMoveStr.IndexOf('.') == -1 ? 0 : minMoveStr.Length - minMoveStr.IndexOf('.') - 1
        };
    }
}