


public static class RsiCalculatorService
{
    private static readonly int _lookbackPeriods = 14;

    public static decimal GetPriceForTargetRsi(List<decimal> prices, decimal targetRsi)
    {
        // Calculate initial average gains and losses
        decimal avgGain = 0;
        decimal avgLoss = 0;

        for (int i = 1; i < _lookbackPeriods; i++)
        {
            decimal priceDiff = prices[i] - prices[i - 1];

            if (priceDiff > 0)
            {
                avgGain += priceDiff;
            }
            else
            {
                avgLoss += Math.Abs(priceDiff);
            }
        }

        avgGain /= _lookbackPeriods;
        avgLoss /= _lookbackPeriods;

        // Calculate RS and RSI
        for (int i = _lookbackPeriods; i < prices.Count; i++)
        {
            decimal priceDiff = prices[i] - prices[i - 1];

            decimal gain = 0;
            decimal loss = 0;

            if (priceDiff > 0)
            {
                gain = priceDiff;
            }
            else
            {
                loss = Math.Abs(priceDiff);
            }

            avgGain = (avgGain * (_lookbackPeriods - 1) + gain) / _lookbackPeriods;
            avgLoss = (avgLoss * (_lookbackPeriods - 1) + loss) / _lookbackPeriods;
        }

        decimal lastRsi = 100 - 100 / (1 + avgGain / avgLoss);

        if (lastRsi == targetRsi)
        {
            return prices[^1];
        }

        decimal targetRs = targetRsi / (100 - targetRsi);

        if (targetRsi > lastRsi)
        {
            // Target gain = Y * (Z2 * (A − 1)) − Z1 * (A − 1)
            decimal targetGain = targetRs * avgLoss * (_lookbackPeriods - 1) -
                                avgGain * (_lookbackPeriods - 1);

            return prices[^1] + targetGain;
        }

        // Target loss = ((Z1 * (A − 1)) / Y) − Z2 * (A − 1)
        decimal targetLoss = avgGain * (_lookbackPeriods - 1) / targetRs -
                            avgLoss * (_lookbackPeriods - 1);

        return prices[^1] - targetLoss;
    }
}
