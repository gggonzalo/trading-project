import {
  IChartApi,
  ISeriesApi,
  MouseEventParams,
  Time,
} from "lightweight-charts";
import { IntervalKey } from "./types";

export function getCrosshairDataPoint(
  series: ISeriesApi<"Candlestick">,
  param: MouseEventParams<Time>,
) {
  if (!param.time) return null;

  const dataPoint = param.seriesData.get(series);
  return dataPoint || null;
}

export function syncCrosshair(
  chart: IChartApi,
  series: ISeriesApi<"Candlestick">,
  dataPoint,
) {
  if (dataPoint) {
    chart.setCrosshairPosition(0, dataPoint.time, series);

    return;
  }
  chart.clearCrosshairPosition();
}

// TODO: Remove when old components are removed
export function mapIntervalToLabel(interval: IntervalKey) {
  switch (interval) {
    case "OneMinute":
      return "1m";
    case "FiveMinutes":
      return "5m";
    case "FifteenMinutes":
      return "15m";
    case "OneHour":
      return "1h";
    case "FourHour":
      return "4h";
    case "OneDay":
      return "1d";
    case "OneWeek":
      return "1w";
    case "OneMonth":
      return "1M";
  }
}

export function convertLocalEpochToUtcDate(localEpoch: number) {
  return new Date(
    new Date(localEpoch * 1000).toLocaleString("en-US", { timeZone: "UTC" }),
  );
}

export function convertCandleEpochToLocal(originalEpoch: number) {
  const d = new Date(originalEpoch * 1000);

  return (
    Date.UTC(
      d.getFullYear(),
      d.getMonth(),
      d.getDate(),
      d.getHours(),
      d.getMinutes(),
      d.getSeconds(),
      d.getMilliseconds(),
    ) / 1000
  );
}
