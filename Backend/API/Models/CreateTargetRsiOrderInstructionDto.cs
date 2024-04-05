using Binance.Net.Enums;

public class CreateTargetRsiOrderInstructionDto
{
    public string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public decimal QuoteQty { get; set; }
    public KlineInterval Interval { get; set; }
    public decimal TargetRsi { get; set; }
}