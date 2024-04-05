
using Binance.Net.Enums;

public class TargetRsiOrderInstruction
{
    public Guid Id { get; set; }
    public string OrderId { get; set; }
    public string Symbol { get; set; }
    public OrderSide Side { get; set; }
    // TODO: Calculate this value in job
    public decimal QuoteQty { get; set; }
    public KlineInterval Interval { get; set; }
    public decimal TargetRsi { get; set; }
}
