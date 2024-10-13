import CandlesStreamingService from "@/services/CandlesStreamingService";
import { Candle } from "@/types";
import useAppStore from "@/useAppStore";
import { convertCandleEpochToLocal, convertLocalEpochToUtcDate } from "@/utils";
import {
  ColorType,
  CrosshairMode,
  IChartApi,
  LogicalRange,
  LogicalRangeChangeEventHandler,
  Time,
  createChart,
} from "lightweight-charts";
import { useEffect, useRef, useState } from "react";

const CANDLES_REQUEST_LIMIT = 500;

function SymbolCandleStickChart() {
  // Store
  const symbolInfo = useAppStore((state) => state.symbolInfo);
  const symbolInfoStatus = useAppStore((state) => state.symbolInfoStatus);
  const interval = useAppStore((state) => state.interval);

  const [isLoadingInitialCandles, setIsLoadingInitialCandles] = useState(false);

  // Refs
  const chartContainer = useRef<HTMLDivElement>(null);
  const chartApi = useRef<IChartApi | null>(null);
  const lastHistoricalDataEndTimeRequested = useRef<number | null>(null);

  // Effects
  // Chart creation and data loading
  useEffect(() => {
    if (!chartContainer.current) return;

    const chart = createChart(chartContainer.current, {
      autoSize: true,
      crosshair: {
        mode: CrosshairMode.Hidden,
      },
      layout: {
        background: { type: ColorType.Solid, color: "white" },
        textColor: "rgba(115, 115, 115)",
      },
      rightPriceScale: {
        borderColor: "rgba(115, 115, 115)",
      },
      timeScale: {
        borderColor: "rgba(115, 115, 115)",
        secondsVisible: false,
        timeVisible: interval.includes("Minute") || interval.includes("Hour"),
      },
    });
    chartApi.current = chart;

    const series = chart.addCandlestickSeries({
      upColor: "#26a69a",
      downColor: "#ef5350",
      borderVisible: false,
      wickUpColor: "#26a69a",
      wickDownColor: "#ef5350",
    });

    if (!symbolInfo)
      return () => {
        chart.remove();
        chartApi.current = null;
      };

    series.applyOptions({
      priceFormat: {
        type: "price",
        precision: symbolInfo.priceFormat.precision,
        minMove: symbolInfo.priceFormat.minMove,
      },
    });

    const dataUpdatesController = new AbortController();

    let candleUpdatesSubscription: { unsubscribe: () => void } | null = null;
    let tryLoadHistoricalCandles: LogicalRangeChangeEventHandler | null = null;

    const loadChartDataSources = async () => {
      // Initial candles
      setIsLoadingInitialCandles(true);

      const initialCandlesResponse = await fetch(
        `http://localhost:5215/candles?symbol=${symbolInfo.symbol}&interval=${interval}&limit=${CANDLES_REQUEST_LIMIT}`,
      );
      const initialCandles = await initialCandlesResponse.json();

      if (dataUpdatesController.signal.aborted) return;

      const localCandles = initialCandles.map((candle: Candle) => ({
        ...candle,
        time: convertCandleEpochToLocal(candle.time),
      }));

      series.setData(localCandles);
      setIsLoadingInitialCandles(false);

      chart.applyOptions({
        crosshair: {
          mode: CrosshairMode.Normal,
        },
      });

      // Candle updates
      candleUpdatesSubscription = CandlesStreamingService.subscribe(
        symbolInfo.symbol,
        interval,
        (candle) => {
          const chartCandle = {
            ...candle,
            time: candle.time as Time,
          };

          series.update(chartCandle);
        },
      );

      // Historical data
      tryLoadHistoricalCandles = async (
        newVisibleLogicalRange: LogicalRange | null,
      ) => {
        if (!newVisibleLogicalRange) return;

        const barsInfo = series.barsInLogicalRange(newVisibleLogicalRange);

        // If there are less than 100 bars before the visible range, try to load more data
        if (barsInfo && barsInfo.barsBefore < 100) {
          const firstBarTime = series.data()[0].time as number;
          const secondBarTime = series.data()[1].time as number;
          const timeDifference = secondBarTime - firstBarTime;

          const newHistoricalDataEndTime = firstBarTime - timeDifference;

          if (
            lastHistoricalDataEndTimeRequested.current ===
            newHistoricalDataEndTime
          )
            return;

          lastHistoricalDataEndTimeRequested.current = newHistoricalDataEndTime;

          const historicalDataResponse = await fetch(
            `http://localhost:5215/candles?symbol=${symbolInfo.symbol}&interval=${interval}&endTime=${convertLocalEpochToUtcDate(newHistoricalDataEndTime).toISOString()}&limit=${CANDLES_REQUEST_LIMIT}`,
          );
          const historicalData = await historicalDataResponse.json();

          if (dataUpdatesController.signal.aborted) return;

          const localCandles = historicalData.map((candle: Candle) => ({
            ...candle,
            time: convertCandleEpochToLocal(candle.time),
          }));

          series.setData([...localCandles, ...series.data()]);
        }
      };

      chart
        .timeScale()
        .subscribeVisibleLogicalRangeChange(tryLoadHistoricalCandles);

      // Initial trigger
      tryLoadHistoricalCandles(chart.timeScale().getVisibleLogicalRange());
    };

    loadChartDataSources();

    return () => {
      dataUpdatesController.abort();
      chart.remove();
      chartApi.current = null;

      // Initial candles cleanup
      setIsLoadingInitialCandles(false);

      // Candle updates cleanup
      candleUpdatesSubscription?.unsubscribe();

      // Historical data cleanup
      if (tryLoadHistoricalCandles)
        chart
          .timeScale()
          .unsubscribeVisibleLogicalRangeChange(tryLoadHistoricalCandles);
    };
  }, [symbolInfo, interval]);

  // Watermark
  useEffect(() => {
    if (symbolInfoStatus === "loading") {
      chartApi.current?.applyOptions({
        watermark: {
          visible: true,
          fontSize: 24,
          horzAlign: "center",
          vertAlign: "center",
          color: "rgba(115, 115, 115)",
          text: "Loading symbol info...",
        },
      });

      return;
    }

    if (!symbolInfo?.symbol) {
      chartApi.current?.applyOptions({
        watermark: {
          visible: true,
          fontSize: 24,
          horzAlign: "center",
          vertAlign: "center",
          color: "rgba(115, 115, 115)",
          text: "Select a symbol",
        },
      });

      return;
    }

    if (isLoadingInitialCandles) {
      chartApi.current?.applyOptions({
        watermark: {
          visible: true,
          fontSize: 24,
          horzAlign: "center",
          vertAlign: "center",
          color: "rgba(115, 115, 115)",
          text: "Loading historical data...",
        },
      });

      return;
    }

    chartApi.current?.applyOptions({
      watermark: {
        visible: false,
      },
    });
  }, [isLoadingInitialCandles, symbolInfo?.symbol, symbolInfoStatus]);

  return <div ref={chartContainer} className="size-full"></div>;
}

export default SymbolCandleStickChart;
