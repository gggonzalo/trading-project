import {
  ColorType,
  CrosshairMode,
  IChartApi,
  ISeriesApi,
  createChart,
} from "lightweight-charts";
import { useEffect, useRef } from "react";

interface ChartProps {
  onApisReady: (
    chartApi: IChartApi,
    seriesApi: ISeriesApi<"Candlestick">,
  ) => void;
}

export default function CandleStickChart({ onApisReady }: ChartProps) {
  const chartContainerRef = useRef<HTMLDivElement | null>(null);
  const chartRef = useRef<IChartApi | null>(null);

  useEffect(() => {
    // console.log("Testing effect running");

    if (chartContainerRef.current) {
      const chartApi = createChart(chartContainerRef.current, {
        autoSize: true,
        crosshair: {
          mode: CrosshairMode.Normal,
        },
        layout: {
          background: { type: ColorType.Solid, color: "white" },
          textColor: "black",
        },
        timeScale: {
          secondsVisible: false,
        },
      });
      chartApi.timeScale().fitContent();

      const seriesApi = chartApi.addCandlestickSeries({
        upColor: "#26a69a",
        downColor: "#ef5350",
        borderVisible: false,
        wickUpColor: "#26a69a",
        wickDownColor: "#ef5350",
      });

      onApisReady(chartApi, seriesApi);

      chartRef.current = chartApi;
    }

    return () => {
      if (chartRef.current) {
        chartRef.current.remove();
      }
    };
  }, [onApisReady]);

  return <div className="h-full w-full" ref={chartContainerRef} />;
}
