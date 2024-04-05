import { Kline, RsiCandle } from "@/types";

export default class RsiCandlesService {
  constructor(private lookbackPeriods: number = 14) {}

  generateRsiCandles(klines: Kline[]): RsiCandle[] {
    const { lookbackPeriods } = this;

    if (klines.length <= lookbackPeriods)
      // Return array with empty objects if there's not enough data
      return Array.from({ length: klines.length }, (_, i) => ({
        time: klines[i].time,
      }));

    const candles: RsiCandle[] = [];

    // Fill the candles array with empty objects for klines that don't have enough data before
    for (let i = 0; i < lookbackPeriods; i++) {
      candles.push({
        time: klines[i].time,
      });
    }

    // Calculate initial average gains and losses
    let avgCloseGain = 0;
    let avgCloseLoss = 0;

    for (let i = 1; i < lookbackPeriods; i++) {
      const closePriceDiff = klines[i].close - klines[i - 1].close;

      if (closePriceDiff > 0) {
        avgCloseGain += closePriceDiff;
      } else {
        avgCloseLoss += Math.abs(closePriceDiff);
      }
    }

    avgCloseGain /= lookbackPeriods;
    avgCloseLoss /= lookbackPeriods;

    // Calculate RS and RSI
    for (let i = lookbackPeriods; i < klines.length; i++) {
      const highPriceDiff = klines[i].high - klines[i - 1].close;
      const lowPriceDiff = klines[i].low - klines[i - 1].close;
      const closePriceDiff = klines[i].close - klines[i - 1].close;

      let highGain = 0;
      let highLoss = 0;
      let lowGain = 0;
      let lowLoss = 0;
      let closeGain = 0;
      let closeLoss = 0;

      if (highPriceDiff > 0) {
        highGain = highPriceDiff;
      } else {
        highLoss = Math.abs(highPriceDiff);
      }

      if (lowPriceDiff > 0) {
        lowGain = lowPriceDiff;
      } else {
        lowLoss = Math.abs(lowPriceDiff);
      }

      if (closePriceDiff > 0) {
        closeGain = closePriceDiff;
      } else {
        closeLoss = Math.abs(closePriceDiff);
      }

      const highAvgGain =
        (avgCloseGain * (lookbackPeriods - 1) + highGain) / lookbackPeriods;
      const highAvgLoss =
        (avgCloseLoss * (lookbackPeriods - 1) + highLoss) / lookbackPeriods;
      const lowAvgGain =
        (avgCloseGain * (lookbackPeriods - 1) + lowGain) / lookbackPeriods;
      const lowAvgLoss =
        (avgCloseLoss * (lookbackPeriods - 1) + lowLoss) / lookbackPeriods;

      avgCloseGain =
        (avgCloseGain * (lookbackPeriods - 1) + closeGain) / lookbackPeriods;
      avgCloseLoss =
        (avgCloseLoss * (lookbackPeriods - 1) + closeLoss) / lookbackPeriods;

      const highRs = highAvgGain / highAvgLoss;
      const lowRs = lowAvgGain / lowAvgLoss;
      const closeRs = avgCloseGain / avgCloseLoss;

      const highRsi = 100 - 100 / (1 + highRs);
      const lowRsi = 100 - 100 / (1 + lowRs);
      const closeRsi = 100 - 100 / (1 + closeRs);

      candles.push({
        time: klines[i].time,
        high: highRsi,
        low: lowRsi,
        close: closeRsi,
      });
    }

    return candles;
  }
}
