import SymbolRsiCandlesCard from "@/components/SymbolRsiCandlesCard";
import { useToast } from "@/components/ui/use-toast";
import {
  IntervalKey,
  IntervalObj,
  RsiCandle,
  SymbolIntervalRsiCandles,
} from "@/types";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { useEffect, useRef, useState } from "react";

const INITIAL_SYMBOLS = [
  "BTCUSDT",
  "INJUSDT",
  "DOTUSDT",
  "ADAUSDT",
  "HBARUSDT",
  "ATOMUSDT",
  "BOMEUSDT",
  "WIFUSDT",
  "1000FLOKIUSDT",
  "ENAUSDT",
];
const INITIAL_SYMBOL_INTERVAL_RSI_CANDLES: SymbolIntervalRsiCandles[] =
  INITIAL_SYMBOLS.map((symbol) => ({
    symbol,
    intervalRsiCandles: [
      {
        interval: "OneMinute",
      },
      {
        interval: "FiveMinutes",
      },
      {
        interval: "FifteenMinutes",
      },
      {
        interval: "OneHour",
      },
      {
        interval: "FourHour",
      },
      {
        interval: "OneDay",
      },
      {
        interval: "OneWeek",
      },
      {
        interval: "OneMonth",
      },
    ],
  }));

interface SymbolIntervalRsiCandleUpdate {
  symbol: string;
  interval: IntervalKey;
  candle?: RsiCandle;
}

function RsiTracker() {
  const [symbolIntervalRsiCandles, setSymbolIntervalRsiCandles] = useState<
    SymbolIntervalRsiCandles[]
  >(INITIAL_SYMBOL_INTERVAL_RSI_CANDLES);

  const { toast } = useToast();

  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5215/rsi-candles-hub")
      .build();
    connectionRef.current = connection;

    // TODO: Test
    connection.on(
      "RsiCandleUpdate",
      (rsiCandleUpdate: SymbolIntervalRsiCandleUpdate) => {
        const { symbol, interval, candle } = rsiCandleUpdate;

        setSymbolIntervalRsiCandles((prev) => {
          const existingSymbolIntervalRsiCandle = prev.find(
            (x) => x.symbol === symbol,
          );

          if (!existingSymbolIntervalRsiCandle)
            return [
              ...prev,
              {
                symbol,
                intervalRsiCandles: [
                  {
                    interval,
                    candle,
                  },
                ],
              },
            ];

          return prev.map((x) => {
            if (x.symbol !== symbol) return x;

            const existingIntervalRsiCandle = x.intervalRsiCandles.find(
              (y) => y.interval === interval,
            );

            if (!existingIntervalRsiCandle)
              return {
                ...x,
                intervalRsiCandles: [
                  ...x.intervalRsiCandles,
                  {
                    interval,
                    candle,
                  },
                ],
              };

            return {
              ...x,
              intervalRsiCandles: x.intervalRsiCandles.map((y) =>
                y.interval !== interval
                  ? y
                  : {
                      ...y,
                      candle,
                    },
              ),
            };
          });
        });
      },
    );

    connection.start().then(() => {
      // TODO: Await this invoke to wait for the backend to full create the subscription? Needed if we want to show a spinner
      connection
        .invoke(
          "SubscribeToRsiCandleUpdates",
          INITIAL_SYMBOLS,
          Object.values(IntervalObj),
        )
        .catch((e) => {
          toast({
            title: "Error subscribing to RSI candle updates.",
            description: e.message,
            variant: "destructive",
          });
        });
    });

    return () => {
      setSymbolIntervalRsiCandles(INITIAL_SYMBOL_INTERVAL_RSI_CANDLES);

      connection.stop();
    };
  }, [toast]);

  return (
    <div className="grid grid-cols-2 gap-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
      {symbolIntervalRsiCandles.map((symbolIntervalRsiCandle) => {
        const { symbol, intervalRsiCandles } = symbolIntervalRsiCandle;

        return (
          <SymbolRsiCandlesCard
            key={symbol}
            symbol={symbol}
            intervalRsiCandles={intervalRsiCandles}
            onIntervalToggle={(interval, enabled) => {
              // TODO: Call streaming endpoint to subscribe/unsubscribe to the symbol and interval
            }}
          />
        );
      })}
    </div>
  );
}

export default RsiTracker;
