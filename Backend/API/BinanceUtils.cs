using Binance.Net.Objects.Models.Spot;

public static class BinanceUtils
{
    public static int GetSymbolPricePrecision(BinanceSymbol symbolInfo)
    {
        if (symbolInfo.PriceFilter == null) return 0;

        var tickSize = symbolInfo.PriceFilter.TickSize.ToString().TrimEnd('0');

        return tickSize.IndexOf('.') == -1 ? 0 : tickSize.Length - tickSize.IndexOf('.') - 1;
    }

    public static int GetSymbolQuantityPrecision(BinanceSymbol symbolInfo)
    {
        if (symbolInfo.LotSizeFilter == null) return 0;

        var stepSize = symbolInfo.LotSizeFilter.StepSize.ToString().TrimEnd('0');

        return stepSize.IndexOf('.') == -1 ? 0 : stepSize.Length - stepSize.IndexOf('.') - 1;
    }
}