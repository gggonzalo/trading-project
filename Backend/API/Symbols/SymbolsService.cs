using Binance.Net.Interfaces.Clients;

public class SymbolsService(IBinanceRestClient binanceRestClient)
{
    public async Task<SymbolInfo?> GetSymbolInfoAsync(string symbol)
    {
        var exchangeInfoResult = await binanceRestClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
        var symbolsExchangeInfo = exchangeInfoResult.Data.Symbols;

        var symbolExchangeInfo = symbolsExchangeInfo.FirstOrDefault(s => s.Name == symbol);

        if (symbolExchangeInfo == null) return null;

        return new SymbolInfo
        {
            Symbol = symbolExchangeInfo.Name,
            BaseAsset = symbolExchangeInfo.BaseAsset,
            QuoteAsset = symbolExchangeInfo.QuoteAsset,
            PriceFormat = BinanceUtils.GetSymbolPriceFormat(symbolExchangeInfo),
            QuantityFormat = BinanceUtils.GetSymbolQuantityFormat(symbolExchangeInfo)
        };
    }
}