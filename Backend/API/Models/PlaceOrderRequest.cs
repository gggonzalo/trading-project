using Binance.Net.Enums;

public class OrderRequest
{
    // TODO: Fix these warnings
    public string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public SpotOrderType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}