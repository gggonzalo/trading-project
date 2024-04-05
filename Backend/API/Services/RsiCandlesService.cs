using Binance.Net.Interfaces;

public class RsiCandlesService(IEnumerable<IBinanceKline> initialKlines)
{
    private readonly int _lookbackPeriods = 14;
    private readonly int _klinesLimit = 500;
    private readonly List<IBinanceKline> _klines = initialKlines.ToList();

    public void Update(IBinanceKline newKline)
    {
        var existingKline = _klines.FirstOrDefault(k => k.OpenTime == newKline.OpenTime);

        if (existingKline != null)
        {
            existingKline.HighPrice = newKline.HighPrice;
            existingKline.LowPrice = newKline.LowPrice;
            existingKline.ClosePrice = newKline.ClosePrice;
        }
        else
        {
            if (_klines.Count >= _klinesLimit)
            {
                _klines.RemoveAt(0);
            }

            // TODO: Add validation to check the time difference between the new kline and the last kline is equal to the difference between the last and second-to-last kline. If not, throw an exception

            _klines.Add(newKline);
        }
    }

    public RsiCandle? GetLastRsiCandle()
    {
        if (_klines.Count <= _lookbackPeriods)
        {
            return null;
        }

        List<RsiCandle> candles = [];

        for (int i = 0; i < _lookbackPeriods; i++)
        {
            candles.Add(new RsiCandle
            {
                Time = _klines[i].OpenTime,
            });
        }

        // TODO: Modify method to not iterate over all klines again and create a list. Instead, calculate the RSI for the last kline and return it
        // Calculate initial average gains and losses
        decimal avgCloseGain = 0;
        decimal avgCloseLoss = 0;

        for (int i = 1; i < _lookbackPeriods; i++)
        {
            decimal closePriceDiff = _klines[i].ClosePrice - _klines[i - 1].ClosePrice;

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
        for (int i = _lookbackPeriods; i < _klines.Count; i++)
        {
            decimal highPriceDiff = _klines[i].HighPrice - _klines[i - 1].ClosePrice;
            decimal lowPriceDiff = _klines[i].LowPrice - _klines[i - 1].ClosePrice;
            decimal closePriceDiff = _klines[i].ClosePrice - _klines[i - 1].ClosePrice;

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
                Time = _klines[i].OpenTime,
                High = highRsi,
                Low = lowRsi,
                Close = closeRsi,
            });
        }

        return candles[^1];
    }
}
