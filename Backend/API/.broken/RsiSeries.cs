public class RsiSeries(IEnumerable<Candle> initialCandles)
{
    private readonly int _lookbackPeriods = 14;
    private readonly int _candlesLimit = 500;
    private readonly List<Candle> _candles = initialCandles.ToList();

    public void Update(Candle newCandle)
    {
        var existingCandleIndex = _candles.FindIndex(c => c.Time == newCandle.Time);

        if (existingCandleIndex != -1)
        {
            _candles[existingCandleIndex] = _candles[existingCandleIndex] with
            {
                High = newCandle.High,
                Low = newCandle.Low,
                Close = newCandle.Close,
            };
        }
        else
        {
            if (_candles.Count >= _candlesLimit)
            {
                _candles.RemoveAt(0);
            }

            // TODO: Add validation to check the time difference between the new candle and the last candle is equal to the difference between the last and second-to-last candle. If not, throw an exception. We could use this exception from outside to recreate the series with the fixed candles

            _candles.Add(newCandle);
        }
    }

    public RsiCandle? GetLastRsiCandle()
    {
        if (_candles.Count <= _lookbackPeriods)
        {
            return null;
        }

        List<RsiCandle> candles = [];

        for (int i = 0; i < _lookbackPeriods; i++)
        {
            candles.Add(new RsiCandle
            {
                Time = _candles[i].Time,
            });
        }


        // TODO: Modify method to not iterate over all candles again and create a list. Instead, calculate the RSI for the last candle and return it
        // Calculate initial average gains and losses
        decimal avgCloseGain = 0;
        decimal avgCloseLoss = 0;

        for (int i = 1; i < _lookbackPeriods; i++)
        {
            decimal closePriceDiff = _candles[i].Close - _candles[i - 1].Close;

            if (closePriceDiff > 0)
            {
                avgCloseGain += closePriceDiff;
            }
            else
            {
                avgCloseLoss += Math.Abs(closePriceDiff);
            }
        }

        avgCloseGain /= _lookbackPeriods;
        avgCloseLoss /= _lookbackPeriods;

        // Calculate RS and RSI
        for (int i = _lookbackPeriods; i < _candles.Count; i++)
        {
            decimal highPriceDiff = _candles[i].High - _candles[i - 1].Close;
            decimal lowPriceDiff = _candles[i].Low - _candles[i - 1].Close;
            decimal closePriceDiff = _candles[i].Close - _candles[i - 1].Close;

            decimal highGain = 0;
            decimal highLoss = 0;
            decimal lowGain = 0;
            decimal lowLoss = 0;
            decimal closeGain = 0;
            decimal closeLoss = 0;

            if (highPriceDiff > 0)
            {
                highGain = highPriceDiff;
            }
            else
            {
                highLoss = Math.Abs(highPriceDiff);
            }

            if (lowPriceDiff > 0)
            {
                lowGain = lowPriceDiff;
            }
            else
            {
                lowLoss = Math.Abs(lowPriceDiff);
            }

            if (closePriceDiff > 0)
            {
                closeGain = closePriceDiff;
            }
            else
            {
                closeLoss = Math.Abs(closePriceDiff);
            }

            decimal highAvgGain = (avgCloseGain * (_lookbackPeriods - 1) + highGain) / _lookbackPeriods;
            decimal highAvgLoss = (avgCloseLoss * (_lookbackPeriods - 1) + highLoss) / _lookbackPeriods;
            decimal lowAvgGain = (avgCloseGain * (_lookbackPeriods - 1) + lowGain) / _lookbackPeriods;
            decimal lowAvgLoss = (avgCloseLoss * (_lookbackPeriods - 1) + lowLoss) / _lookbackPeriods;

            avgCloseGain = (avgCloseGain * (_lookbackPeriods - 1) + closeGain) / _lookbackPeriods;
            avgCloseLoss = (avgCloseLoss * (_lookbackPeriods - 1) + closeLoss) / _lookbackPeriods;

            decimal highRs = highAvgGain / highAvgLoss;
            decimal lowRs = lowAvgGain / lowAvgLoss;
            decimal closeRs = avgCloseGain / avgCloseLoss;

            decimal highRsi = 100 - 100 / (1 + highRs);
            decimal lowRsi = 100 - 100 / (1 + lowRs);
            decimal closeRsi = 100 - 100 / (1 + closeRs);

            candles.Add(new RsiCandle
            {
                Time = _candles[i].Time,
                High = highRsi,
                Low = lowRsi,
                Close = closeRsi,
            });
        }

        return candles[^1];
    }
}
